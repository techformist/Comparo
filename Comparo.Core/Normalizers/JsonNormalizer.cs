using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Comparo.Core.Normalizers;

public class JsonNormalizer
{
    private readonly bool _sortProperties;
    private readonly bool _normalizeWhitespace;
    private readonly bool _normalizeTypes;

    public JsonNormalizer(bool sortProperties = true, bool normalizeWhitespace = true, bool normalizeTypes = true)
    {
        _sortProperties = sortProperties;
        _normalizeWhitespace = normalizeWhitespace;
        _normalizeTypes = normalizeTypes;
    }

    public async Task<string> NormalizeAsync(string jsonContent, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => Normalize(jsonContent), cancellationToken);
    }

    public string Normalize(string jsonContent)
    {
        var jToken = JToken.Parse(jsonContent);
        var normalized = NormalizeToken(jToken);
        return normalized.ToString(Formatting.Indented);
    }

    public async Task<string> NormalizeFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var content = await File.ReadAllTextAsync(filePath, cancellationToken);
        return await NormalizeAsync(content, cancellationToken);
    }

    private JToken NormalizeToken(JToken token)
    {
        return token.Type switch
        {
            JTokenType.Object => NormalizeObject((JObject)token),
            JTokenType.Array => NormalizeArray((JArray)token),
            JTokenType.String => NormalizeString((JValue)token),
            JTokenType.Integer or JTokenType.Float => NormalizeNumber((JValue)token),
            JTokenType.Boolean => NormalizeBoolean((JValue)token),
            JTokenType.Null => NormalizeNull((JValue)token),
            _ => token.DeepClone()
        };
    }

    private JObject NormalizeObject(JObject obj)
    {
        var normalized = new JObject();

        var properties = _sortProperties
            ? obj.Properties().OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            : obj.Properties();

        foreach (var property in properties)
        {
            normalized.Add(property.Name, NormalizeToken(property.Value));
        }

        return normalized;
    }

    private JArray NormalizeArray(JArray array)
    {
        var normalized = new JArray();

        foreach (var item in array)
        {
            normalized.Add(NormalizeToken(item));
        }

        return normalized;
    }

    private JValue NormalizeString(JValue value)
    {
        if (!_normalizeWhitespace)
        {
            return (JValue)value.DeepClone();
        }

        var stringValue = value.Value<string>();
        if (stringValue == null)
        {
            return (JValue)value.DeepClone();
        }

        var normalized = stringValue.Trim();
        return new JValue(normalized);
    }

    private JValue NormalizeNumber(JValue value)
    {
        if (!_normalizeTypes)
        {
            return (JValue)value.DeepClone();
        }

        if (value.Type == JTokenType.Integer)
        {
            return new JValue(value.Value<long>());
        }

        if (value.Type == JTokenType.Float)
        {
            var floatValue = value.Value<double>();
            if (double.IsInteger(floatValue))
            {
                return new JValue((long)floatValue);
            }
            return new JValue(floatValue);
        }

        return (JValue)value.DeepClone();
    }

    private JValue NormalizeBoolean(JValue value)
    {
        return _normalizeTypes ? new JValue(value.Value<bool>()) : (JValue)value.DeepClone();
    }

    private JValue NormalizeNull(JValue value)
    {
        return (JValue)value.DeepClone();
    }

    public static async Task<string> CanonicalizeAsync(string jsonContent, CancellationToken cancellationToken = default)
    {
        var normalizer = new JsonNormalizer(sortProperties: true, normalizeWhitespace: true, normalizeTypes: false);
        return await normalizer.NormalizeAsync(jsonContent, cancellationToken);
    }
}
