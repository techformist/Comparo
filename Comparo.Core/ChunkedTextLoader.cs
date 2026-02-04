using System.Collections.Concurrent;
using System.Text;

namespace Comparo.Core;

public class ChunkedTextLoader : IDisposable
{
    private readonly string _filePath;
    private readonly int _chunkSize;
    private readonly int _maxCachedChunks;
    private readonly ConcurrentDictionary<int, Chunk> _chunkCache;
    private readonly LinkedList<int> _lruList;
    private readonly object _lruLock = new();
    private readonly SemaphoreSlim _fileAccessLock = new(1, 1);
    private List<long>? _lineOffsets;
    private int? _totalLineCount;
    private long _fileSize;
    private bool _disposed;

    public string FilePath => _filePath;
    public long FileSize => _fileSize;
    public long EstimatedMemoryUsage { get; private set; }
    public int TotalLineCount => GetTotalLineCount();

    public ChunkedTextLoader(string filePath, int chunkSize = 10000, int maxCachedChunks = 5)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _chunkSize = chunkSize > 0 ? chunkSize : 10000;
        _maxCachedChunks = maxCachedChunks > 0 ? maxCachedChunks : 5;
        _chunkCache = new ConcurrentDictionary<int, Chunk>();
        _lruList = new LinkedList<int>();

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        _fileSize = new FileInfo(filePath).Length;
    }

    public string? GetLine(int lineNumber, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (lineNumber < 0)
            throw new ArgumentOutOfRangeException(nameof(lineNumber), "Line number must be non-negative");

        if (lineNumber >= TotalLineCount)
            return null;

        var chunkNumber = lineNumber / _chunkSize;
        var chunk = LoadChunk(chunkNumber, cancellationToken);
        var lineIndex = lineNumber % _chunkSize;

        return chunk.Lines[lineIndex];
    }

    public string[] GetLineRange(int startLine, int count, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (startLine < 0)
            throw new ArgumentOutOfRangeException(nameof(startLine), "Start line must be non-negative");

        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative");

        var endLine = Math.Min(startLine + count - 1, TotalLineCount - 1);
        if (startLine > endLine)
            return Array.Empty<string>();

        var result = new List<string>(count);
        var currentLine = startLine;

        while (currentLine <= endLine)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var chunkNumber = currentLine / _chunkSize;
            var chunk = LoadChunk(chunkNumber, cancellationToken);
            var chunkStartIndex = currentLine % _chunkSize;
            var chunkEndIndex = Math.Min(chunkStartIndex + (endLine - currentLine), _chunkSize - 1);

            for (var i = chunkStartIndex; i <= chunkEndIndex && currentLine <= endLine; i++)
            {
                result.Add(chunk.Lines[i]);
                currentLine++;
            }
        }

        return result.ToArray();
    }

    public int GetTotalLineCount()
    {
        if (_totalLineCount.HasValue)
            return _totalLineCount.Value;

        BuildLineOffsetIndex();
        return _totalLineCount!.Value;
    }

    private void BuildLineOffsetIndex()
    {
        if (_lineOffsets != null)
            return;

        var offsets = new List<long> { 0 };

        using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        while (reader.ReadLine() != null)
        {
            offsets.Add(stream.Position);
        }

        _lineOffsets = offsets;
        _totalLineCount = offsets.Count - 1;
    }

    private Chunk LoadChunk(int chunkNumber, CancellationToken cancellationToken = default)
    {
        if (_chunkCache.TryGetValue(chunkNumber, out var cachedChunk))
        {
            UpdateLru(chunkNumber);
            return cachedChunk;
        }

        var lines = ReadChunkLines(chunkNumber, cancellationToken);
        var chunk = new Chunk(lines, DateTime.UtcNow);

        while (_chunkCache.Count >= _maxCachedChunks)
        {
            EvictLeastRecentlyUsed();
        }

        _chunkCache[chunkNumber] = chunk;
        lock (_lruLock)
        {
            _lruList.AddFirst(chunkNumber);
        }

        UpdateMemoryUsage();
        return chunk;
    }

    private string[] ReadChunkLines(int chunkNumber, CancellationToken cancellationToken = default)
    {
        BuildLineOffsetIndex();

        if (_lineOffsets == null || _totalLineCount == null)
        {
            throw new InvalidOperationException("Line offset index not built");
        }

        var startLine = chunkNumber * _chunkSize;
        var endLine = Math.Min(startLine + _chunkSize, _totalLineCount.Value);
        var lines = new List<string>(endLine - startLine);

        _fileAccessLock.Wait(cancellationToken);
        try
        {
            using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

            // Seek to the start of the first line in this chunk
            stream.Seek(_lineOffsets[startLine], SeekOrigin.Begin);
            reader.DiscardBufferedData();

            for (var i = startLine; i < endLine; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var line = reader.ReadLine();
                if (line != null)
                {
                    lines.Add(line);
                }
                else
                {
                    break;
                }
            }
        }
        finally
        {
            _fileAccessLock.Release();
        }

        return lines.ToArray();
    }

    private void UpdateLru(int chunkNumber)
    {
        lock (_lruLock)
        {
            var node = _lruList.Find(chunkNumber);
            if (node != null)
            {
                _lruList.Remove(node);
                _lruList.AddFirst(node);
            }
        }
    }

    private void EvictLeastRecentlyUsed()
    {
        lock (_lruLock)
        {
            if (_lruList.Count == 0)
                return;

            var lruChunk = _lruList.Last?.Value;
            if (lruChunk.HasValue)
            {
                _lruList.RemoveLast();
                _chunkCache.TryRemove(lruChunk.Value, out _);
            }
        }

        UpdateMemoryUsage();
    }

    private void UpdateMemoryUsage()
    {
        var totalMemory = 0L;

        foreach (var chunk in _chunkCache.Values)
        {
            totalMemory += chunk.EstimatedSize;
        }

        EstimatedMemoryUsage = totalMemory;
    }

    public void ClearCache()
    {
        _chunkCache.Clear();

        lock (_lruLock)
        {
            _lruList.Clear();
        }

        EstimatedMemoryUsage = 0;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        ClearCache();
        _fileAccessLock.Dispose();
        _disposed = true;
    }

    private class Chunk
    {
        public string[] Lines { get; }
        public DateTime LoadTime { get; }
        public long EstimatedSize { get; }

        public Chunk(string[] lines, DateTime loadTime)
        {
            Lines = lines;
            LoadTime = loadTime;

            EstimatedSize = lines.Sum(line => line.Length * 2L);
        }
    }
}
