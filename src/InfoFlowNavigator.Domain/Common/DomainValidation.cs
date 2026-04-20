namespace InfoFlowNavigator.Domain.Common;

internal static class DomainValidation
{
    public static string Required(string? value, string paramName, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message, paramName);
        }

        return value.Trim();
    }

    public static IReadOnlyList<string> NormalizeTags(IEnumerable<string>? tags)
    {
        return tags?
            .Where(static tag => !string.IsNullOrWhiteSpace(tag))
            .Select(static tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    public static IReadOnlyDictionary<string, string> NormalizeMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in metadata)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            normalized[key.Trim()] = value?.Trim() ?? string.Empty;
        }

        return normalized;
    }

    public static double? NormalizeConfidence(double? confidence, string paramName)
    {
        if (confidence is null)
        {
            return null;
        }

        if (confidence < 0d || confidence > 1d)
        {
            throw new ArgumentOutOfRangeException(paramName, "Confidence must be between 0.0 and 1.0.");
        }

        return confidence;
    }
}
