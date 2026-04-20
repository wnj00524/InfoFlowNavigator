using InfoFlowNavigator.Domain.Common;

namespace InfoFlowNavigator.Domain.Hypotheses;

public sealed record Hypothesis(
    Guid Id,
    string Title,
    string Statement,
    HypothesisStatus Status,
    double? Confidence,
    string? Notes,
    IReadOnlyList<string> Tags,
    IReadOnlyDictionary<string, string> Metadata,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    public static Hypothesis Create(
        string title,
        string statement,
        HypothesisStatus status = HypothesisStatus.Draft,
        double? confidence = null,
        string? notes = null,
        IEnumerable<string>? tags = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        var now = DateTimeOffset.UtcNow;

        return new Hypothesis(
            Guid.NewGuid(),
            DomainValidation.Required(title, nameof(title), "Hypothesis title is required."),
            DomainValidation.Required(statement, nameof(statement), "Hypothesis statement is required."),
            status,
            DomainValidation.NormalizeConfidence(confidence, nameof(confidence)),
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            DomainValidation.NormalizeTags(tags),
            DomainValidation.NormalizeMetadata(metadata),
            now,
            now);
    }

    public Hypothesis Update(
        string title,
        string statement,
        HypothesisStatus status,
        double? confidence = null,
        string? notes = null,
        IEnumerable<string>? tags = null,
        IReadOnlyDictionary<string, string>? metadata = null) =>
        this with
        {
            Title = DomainValidation.Required(title, nameof(title), "Hypothesis title is required."),
            Statement = DomainValidation.Required(statement, nameof(statement), "Hypothesis statement is required."),
            Status = status,
            Confidence = DomainValidation.NormalizeConfidence(confidence, nameof(confidence)),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            Tags = DomainValidation.NormalizeTags(tags),
            Metadata = DomainValidation.NormalizeMetadata(metadata),
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
}
