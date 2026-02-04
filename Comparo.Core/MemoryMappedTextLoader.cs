using System.IO.MemoryMappedFiles;
using System.Text;

namespace Comparo.Core;

/// <summary>
/// Memory-mapped file loader for very large files (>100MB).
/// Uses true memory-mapped I/O with line offset indexing for O(1) line lookup.
/// Supports files up to Int64.MaxValue in size.
/// </summary>
public class MemoryMappedTextLoader : IDisposable
{
    private readonly string _filePath;
    private readonly MemoryMappedFile _memoryMappedFile;
    private readonly MemoryMappedViewAccessor _accessor;
    private readonly List<long> _lineOffsets;
    private readonly long _fileSize;
    private readonly long _maxFileSize;
    private bool _disposed;

    public string FilePath => _filePath;
    public long FileSize => _fileSize;
    public int TotalLineCount => _lineOffsets.Count - 1;

    public MemoryMappedTextLoader(string filePath, long maxFileSize = 10L * 1024 * 1024 * 1024) // 10GB default
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _maxFileSize = maxFileSize;

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        var fileInfo = new FileInfo(filePath);
        _fileSize = fileInfo.Length;

        if (_fileSize > _maxFileSize)
        {
            throw new ArgumentException($"File size ({_fileSize:N0} bytes) exceeds maximum allowed size ({_maxFileSize:N0} bytes)");
        }

        if (_fileSize == 0)
        {
            _lineOffsets = new List<long> { 0 };
            _memoryMappedFile = null!;
            _accessor = null!;
            return;
        }

        _memoryMappedFile = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
        _accessor = _memoryMappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
        _lineOffsets = BuildLineOffsetIndex();
    }

    public string? GetLine(int lineNumber)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (lineNumber < 0)
            throw new ArgumentOutOfRangeException(nameof(lineNumber), "Line number must be non-negative");

        if (lineNumber >= TotalLineCount)
            return null;

        if (_fileSize == 0)
            return string.Empty;

        var startOffset = _lineOffsets[lineNumber];
        var endOffset = _lineOffsets[lineNumber + 1];
        var length = (int)(endOffset - startOffset);

        // Remove trailing newline characters
        if (length > 0 && startOffset + length < _fileSize)
        {
            var nextByte = _accessor.ReadByte(startOffset + length);
            if (nextByte == '\n' || nextByte == '\r')
            {
                length--;
                if (length > 0 && nextByte == '\n')
                {
                    var prevByte = _accessor.ReadByte(startOffset + length - 1);
                    if (prevByte == '\r')
                        length--;
                }
            }
        }

        if (length <= 0)
            return string.Empty;

        var buffer = new byte[length];
        _accessor.ReadArray(startOffset, buffer, 0, length);
        return Encoding.UTF8.GetString(buffer);
    }

    public long? GetLineOffset(int lineNumber)
    {
        if (lineNumber < 0 || lineNumber >= TotalLineCount)
            return null;

        return _lineOffsets[lineNumber];
    }

    public long? GetLineLength(int lineNumber)
    {
        if (lineNumber < 0 || lineNumber >= TotalLineCount)
            return null;

        return _lineOffsets[lineNumber + 1] - _lineOffsets[lineNumber];
    }

    private List<long> BuildLineOffsetIndex()
    {
        var offsets = new List<long> { 0 };
        long position = 0;

        const int bufferSize = 65536;
        var buffer = new byte[bufferSize];

        while (position < _fileSize)
        {
            var remaining = _fileSize - position;
            var toRead = (int)Math.Min(remaining, bufferSize);
            _accessor.ReadArray(position, buffer, 0, toRead);

            for (var i = 0; i < toRead; i++)
            {
                if (buffer[i] == '\n')
                {
                    offsets.Add(position + i + 1);
                }
            }

            position += toRead;
        }

        // Add final offset if file doesn't end with newline
        if (_fileSize > 0 && offsets[^1] != _fileSize)
        {
            offsets.Add(_fileSize);
        }

        return offsets;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _accessor?.Dispose();
        _memoryMappedFile?.Dispose();
        _disposed = true;
    }
}
