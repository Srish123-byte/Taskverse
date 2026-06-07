using System.Text.Json;

namespace Taskverse.Data.Utilities;

public static class QuestionAnswerJsonHelper
{
    public static List<string> ParseStoredAnswers(string? storedValue)
    {
        var normalizedValue = NormalizeSingleValue(storedValue);
        if (normalizedValue is null)
        {
            return [];
        }

        try
        {
            var parsedValues = JsonSerializer.Deserialize<List<string>>(normalizedValue);
            return NormalizeAnswerValues(parsedValues);
        }
        catch (JsonException)
        {
            return [normalizedValue];
        }
    }

    public static List<string> NormalizeAnswerValues(IEnumerable<string>? values)
    {
        return (values ?? [])
            .Select(NormalizeSingleValue)
            .OfType<string>()
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string? SerializeAnswers(IEnumerable<string>? values)
    {
        var normalizedValues = NormalizeAnswerValues(values);
        return normalizedValues.Count switch
        {
            0 => null,
            1 => normalizedValues[0],
            _ => JsonSerializer.Serialize(normalizedValues)
        };
    }

    public static string? NormalizeSingleValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return string.Join(
            " ",
            value.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
