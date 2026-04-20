using InfoFlowNavigator.Domain.Common;

namespace InfoFlowNavigator.Domain.Entities;

public sealed record Entity(
    Guid Id,
    string Name,
    string EntityType,
    string? Notes,
    double? Confidence,
    IReadOnlyList<string> Tags,
    IReadOnlyDictionary<string, string> Metadata,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    public static Entity Create(
        string name,
        string entityType,
        string? notes = null,
        double? confidence = null,
        IEnumerable<string>? tags = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        var now = DateTimeOffset.UtcNow;

        return new Entity(
            Guid.NewGuid(),
            DomainValidation.Required(name, nameof(name), "Entity name is required."),
            DomainValidation.Required(entityType, nameof(entityType), "Entity type is required."),
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            DomainValidation.NormalizeConfidence(confidence, nameof(confidence)),
            DomainValidation.NormalizeTags(tags),
            DomainValidation.NormalizeMetadata(metadata),
            now,
            now);
    }

    public Entity Update(
        string name,
        string entityType,
        string? notes = null,
        double? confidence = null,
        IEnumerable<string>? tags = null,
        IReadOnlyDictionary<string, string>? metadata = null) =>
        this with
        {
            Name = DomainValidation.Required(name, nameof(name), "Entity name is required."),
            EntityType = DomainValidation.Required(entityType, nameof(entityType), "Entity type is required."),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            Confidence = DomainValidation.NormalizeConfidence(confidence, nameof(confidence)),
            Tags = DomainValidation.NormalizeTags(tags),
            Metadata = DomainValidation.NormalizeMetadata(metadata),
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
}
