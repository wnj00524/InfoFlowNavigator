using InfoFlowNavigator.Domain.Common;

namespace InfoFlowNavigator.Domain.Relationships;

public sealed record Relationship(
    Guid Id,
    Guid SourceEntityId,
    Guid TargetEntityId,
    string RelationshipType,
    string? Notes,
    double? Confidence,
    IReadOnlyList<string> Tags,
    IReadOnlyDictionary<string, string> Metadata,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    public static Relationship Create(
        Guid sourceEntityId,
        Guid targetEntityId,
        string relationshipType,
        string? notes = null,
        double? confidence = null,
        IEnumerable<string>? tags = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (sourceEntityId == Guid.Empty)
        {
            throw new ArgumentException("Source entity id is required.", nameof(sourceEntityId));
        }

        if (targetEntityId == Guid.Empty)
        {
            throw new ArgumentException("Target entity id is required.", nameof(targetEntityId));
        }

        var now = DateTimeOffset.UtcNow;

        return new Relationship(
            Guid.NewGuid(),
            sourceEntityId,
            targetEntityId,
            DomainValidation.Required(relationshipType, nameof(relationshipType), "Relationship type is required."),
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            DomainValidation.NormalizeConfidence(confidence, nameof(confidence)),
            DomainValidation.NormalizeTags(tags),
            DomainValidation.NormalizeMetadata(metadata),
            now,
            now);
    }
}
