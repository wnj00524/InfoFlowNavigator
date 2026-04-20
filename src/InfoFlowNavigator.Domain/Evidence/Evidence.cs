using InfoFlowNavigator.Domain.Common;

namespace InfoFlowNavigator.Domain.Evidence;

public sealed record Evidence(
    Guid Id,
    string Title,
    string? Citation,
    string? Notes,
    double? Confidence,
    IReadOnlyList<string> Tags,
    IReadOnlyDictionary<string, string> Metadata,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    public static Evidence Create(
        string title,
        string? citation = null,
        string? notes = null,
        double? confidence = null,
        IEnumerable<string>? tags = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        var now = DateTimeOffset.UtcNow;

        return new Evidence(
            Guid.NewGuid(),
            DomainValidation.Required(title, nameof(title), "Evidence title is required."),
            string.IsNullOrWhiteSpace(citation) ? null : citation.Trim(),
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            DomainValidation.NormalizeConfidence(confidence, nameof(confidence)),
            DomainValidation.NormalizeTags(tags),
            DomainValidation.NormalizeMetadata(metadata),
            now,
            now);
    }
}
