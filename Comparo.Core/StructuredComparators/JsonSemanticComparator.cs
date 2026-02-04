using Comparo.Core.DiffModels;
using Newtonsoft.Json.Linq;

namespace Comparo.Core.StructuredComparators;

public class JsonSemanticComparator : IStructuredComparator
{
    public JsonSemanticComparator() { }

    public string GetFileExtension()
    {
        return ".json";
    }

    public JsonPathChange[] Compare(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left))
            throw new ArgumentException("Left JSON input cannot be null or empty", nameof(left));
        if (string.IsNullOrWhiteSpace(right))
            throw new ArgumentException("Right JSON input cannot be null or empty", nameof(right));

        try
        {
            var leftToken = JToken.Parse(left);
            var rightToken = JToken.Parse(right);

            if (leftToken == null || rightToken == null)
                throw new InvalidOperationException("Parsed JSON resulted in null token");

            var changes = new List<JsonPathChange>();
            DiffTokens(leftToken, rightToken, "", changes);
            return changes.ToArray();
        }
        catch (Newtonsoft.Json.JsonException ex)
        {
            throw new InvalidOperationException("Failed to parse JSON: " + ex.Message, ex);
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidOperationException)
        {
            throw new InvalidOperationException("Failed to compare JSON", ex);
        }
    }

    private void DiffTokens(JToken? left, JToken? right, string path, List<JsonPathChange> changes)
    {
        if (left == null && right == null) return;
        if (left == null && right != null)
        {
            changes.Add(new JsonPathChange(JsonPathOperation.Add, path, right));
            return;
        }
        if (left != null && right == null)
        {
            changes.Add(new JsonPathChange(JsonPathOperation.Remove, path));
            return;
        }

        if (left!.Type != right!.Type)
        {
            changes.Add(new JsonPathChange(JsonPathOperation.Replace, path, right) { OldValue = left });
            return;
        }

        // Scalars
        if (left is JValue lv && right is JValue rv)
        {
            if (!JToken.DeepEquals(lv, rv))
                changes.Add(new JsonPathChange(JsonPathOperation.Replace, path, rv) { OldValue = lv });
            return;
        }

        // Objects (order-insensitive)
        if (left is JObject lo && right is JObject ro)
        {
            var names = new HashSet<string>(lo.Properties().Select(p => p.Name));
            names.UnionWith(ro.Properties().Select(p => p.Name));
            foreach (var name in names)
            {
                var lprop = lo.Property(name);
                var rprop = ro.Property(name);
                var newPath = string.IsNullOrEmpty(path) ? name : $"{path}/{name}";

                if (lprop == null && rprop != null)
                {
                    DiffTokens(null, rprop.Value, newPath, changes);
                }
                else if (lprop != null && rprop == null)
                {
                    DiffTokens(lprop.Value, null, newPath, changes);
                }
                else
                {
                    DiffTokens(lprop!.Value, rprop!.Value, newPath, changes);
                }
            }
            return;
        }

        // Arrays
        if (left is JArray la && right is JArray ra)
        {
            if (JToken.DeepEquals(la, ra)) return;

            if (IsPermutation(la, ra))
            {
                changes.Add(new JsonPathChange(JsonPathOperation.Move, path));
                return;
            }

            var max = Math.Max(la.Count, ra.Count);
            for (int i = 0; i < max; i++)
            {
                var newPath = $"{path}[{i}]";
                DiffTokens(i < la.Count ? la[i] : null, i < ra.Count ? ra[i] : null, newPath, changes);
            }
            return;
        }

        // Fallback replace for mismatched complex structures
        if (!JToken.DeepEquals(left, right))
        {
            changes.Add(new JsonPathChange(JsonPathOperation.Replace, path, right, null) { OldValue = left });
        }
    }

    private bool IsPermutation(JArray a, JArray b)
    {
        if (a.Count != b.Count) return false;

        // Use deterministic ordering (ordinal string comparison) for consistent results
        var left = a.Select(t => t.ToString(Newtonsoft.Json.Formatting.None))
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToArray();
        var right = b.Select(t => t.ToString(Newtonsoft.Json.Formatting.None))
                     .OrderBy(x => x, StringComparer.Ordinal)
                     .ToArray();

        for (int i = 0; i < left.Length; i++)
        {
            if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
                return false;
        }

        // If order differs but same multiset, treat as move
        return !JToken.DeepEquals(a, b);
    }
}
