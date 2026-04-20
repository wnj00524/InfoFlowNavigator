using InfoFlowNavigator.Domain.Common;

namespace InfoFlowNavigator.Domain.Events;

public sealed record Event(
    Guid Id,
    string Title,
    DateTimeOffset? OccurredAtUtc,
    string? Notes,
    double? Confidence,
    IReadOnlyList<string> Tags,
    IReadOnlyDictionary<string, string> Metadata,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    public static Event Create(
        string title,
        DateTimeOffset? occurredAtUtc = null,
        string? notes = null,
        double? confidence = null,
        IEnumerable<string>? tags = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        var now = DateTimeOffset.UtcNow;

        return new Event(
            Guid.NewGuid(),
            DomainValidation.Required(title, nameof(title), "Event title is required."),
            occurredAtUtc,
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            DomainValidation.NormalizeConfidence(confidence, nameof(confidence)),
            DomainValidation.NormalizeTags(tags),
            DomainValidation.NormalizeMetadata(metadata),
            now,
            now);
    }

    public Event Update(
        string title,
        DateTimeOffset? occurredAtUtc = null,
        string? notes = null,
        double? confidence = null,
        IEnumerable<string>? tags = null,
        IReadOnlyDictionary<string, string>? metadata = null) =>
        this with
        {
            Title = DomainValidation.Required(title, nameof(title), "Event title is required."),
            OccurredAtUtc = occurredAtUtc,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            Confidence = DomainValidation.NormalizeConfidence(confidence, nameof(confidence)),
            Tags = DomainValidation.NormalizeTags(tags),
            Metadata = DomainValidation.NormalizeMetadata(metadata),
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
}
