using System.Text;

namespace Comparo.Tests.Performance;

public static class TestDataGenerator
{
    private static readonly Random Random = new(42);

    public static string[] GenerateTextLines(int lineCount, int avgLineLength = 50)
    {
        var lines = new string[lineCount];
        for (int i = 0; i < lineCount; i++)
        {
            lines[i] = GenerateLine(i, avgLineLength);
        }
        return lines;
    }

    public static string[] GenerateMarkdownLines(int lineCount)
    {
        var lines = new string[lineCount];
        for (int i = 0; i < lineCount; i++)
        {
            lines[i] = GenerateMarkdownLine(i);
        }
        return lines;
    }

    public static string GenerateJson(int sizeInBytes, int nestingLevel = 3)
    {
        var sb = new StringBuilder();
        GenerateJsonObject(sb, nestingLevel, sizeInBytes, 0);
        return sb.ToString();
    }

    public static string GenerateXml(int sizeInBytes, int depth = 3)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        GenerateXmlElement(sb, "root", depth, sizeInBytes, 0);
        return sb.ToString();
    }

    public static string[] ModifyLines(string[] original, double changePercentage, bool reordering = false)
    {
        var modified = original.ToArray();
        int changeCount = (int)(original.Length * changePercentage);

        if (reordering)
        {
            var indices = Enumerable.Range(0, changeCount).ToList();
            Shuffle(indices);
            
            for (int i = 0; i < changeCount; i++)
            {
                int targetIdx = indices[i];
                if (targetIdx < modified.Length)
                {
                    modified[targetIdx] = original[original.Length - 1 - targetIdx];
                }
            }
        }
        else
        {
            for (int i = 0; i < changeCount; i++)
            {
                int idx = Random.Next(original.Length);
                modified[idx] = GenerateLine(idx, 50) + " [MODIFIED]";
            }
        }

        return modified;
    }

    public static string ModifyJson(string original, double changePercentage)
    {
        var modified = new StringBuilder(original);
        int changeCount = (int)(original.Length * changePercentage);

        for (int i = 0; i < changeCount; i++)
        {
            int pos = Random.Next(10, original.Length - 10);
            modified[pos] = (char)('a' + Random.Next(26));
        }

        return modified.ToString();
    }

    public static string ModifyXml(string original, double changePercentage)
    {
        var modified = new StringBuilder(original);
        int changeCount = (int)(original.Length * changePercentage);

        for (int i = 0; i < changeCount; i++)
        {
            int pos = Random.Next(10, original.Length - 10);
            modified[pos] = (char)('a' + Random.Next(26));
        }

        return modified.ToString();
    }

    private static string GenerateLine(int lineIndex, int length)
    {
        var words = new List<string>();
        int currentLength = 0;

        while (currentLength < length)
        {
            int wordLength = Random.Next(3, 12);
            var word = new string(Enumerable.Range(0, wordLength)
                .Select(_ => (char)('a' + Random.Next(26)))
                .ToArray());
            words.Add(word);
            currentLength += wordLength + 1;
        }

        return $"Line {lineIndex}: {string.Join(" ", words)}";
    }

    private static string GenerateMarkdownLine(int lineIndex)
    {
        int type = lineIndex % 10;
        return type switch
        {
            0 => $"# Heading {lineIndex / 10}",
            1 => $"## Subheading {lineIndex / 10}",
            2 => $"### Sub-subheading {lineIndex / 10}",
            3 => $"- List item {lineIndex}",
            4 => $"1. Numbered item {lineIndex}",
            5 => $"*Italic text* in line {lineIndex}",
            6 => $"**Bold text** in line {lineIndex}",
            7 => $"`Code snippet` at line {lineIndex}",
            8 => $"> Quote {lineIndex}",
            _ => $"Normal paragraph text at line {lineIndex}. {GenerateLine(lineIndex, 40)}"
        };
    }

    private static void GenerateJsonObject(StringBuilder sb, int currentDepth, int targetSize, int currentSize)
    {
        sb.Append('{');

        int propCount = Math.Min(10, Math.Max(1, (targetSize - currentSize) / 100));

        for (int i = 0; i < propCount && currentSize < targetSize; i++)
        {
            if (i > 0) sb.Append(',');

            string propName = $"property{i}";
            sb.Append($"\"{propName}\":");

            if (currentDepth > 0 && Random.NextDouble() > 0.5)
            {
                GenerateJsonObject(sb, currentDepth - 1, targetSize, currentSize);
            }
            else
            {
                string value = GenerateRandomJsonValue();
                sb.Append(value);
                currentSize += value.Length;
            }

            currentSize += propName.Length + 5;
        }

        sb.Append('}');
    }

    private static void GenerateXmlElement(StringBuilder sb, string elementName, int currentDepth, int targetSize, int currentSize)
    {
        sb.Append($"<{elementName}>");

        int childCount = Math.Min(10, Math.Max(1, (targetSize - currentSize) / 100));

        for (int i = 0; i < childCount && currentSize < targetSize; i++)
        {
            if (currentDepth > 0 && Random.NextDouble() > 0.5)
            {
                GenerateXmlElement(sb, $"child{i}", currentDepth - 1, targetSize, currentSize);
            }
            else
            {
                sb.Append($"<item{i}>{GenerateRandomXmlValue()}</item{i}>");
            }
        }

        sb.Append($"</{elementName}>");
    }

    private static string GenerateRandomJsonValue()
    {
        int type = Random.Next(5);
        return type switch
        {
            0 => $"\"{GenerateLine(0, 10)}\"",
            1 => Random.Next(1000).ToString(),
            2 => (Random.NextDouble() > 0.5).ToString().ToLower(),
            3 => "null",
            _ => Random.Next(100).ToString()
        };
    }

    private static string GenerateRandomXmlValue()
    {
        return GenerateLine(0, Random.Next(5, 20));
    }

    private static void Shuffle<T>(IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    public static (string[], string[]) GenerateFilePair(int lineCount, double changePercentage)
    {
        var original = GenerateTextLines(lineCount);
        var modified = ModifyLines(original, changePercentage);
        return (original, modified);
    }

    public static (string, string) GenerateJsonFilePair(int sizeInBytes, double changePercentage)
    {
        var original = GenerateJson(sizeInBytes);
        var modified = ModifyJson(original, changePercentage);
        return (original, modified);
    }

    public static (string, string) GenerateXmlFilePair(int sizeInBytes, double changePercentage)
    {
        var original = GenerateXml(sizeInBytes);
        var modified = ModifyXml(original, changePercentage);
        return (original, modified);
    }
}
