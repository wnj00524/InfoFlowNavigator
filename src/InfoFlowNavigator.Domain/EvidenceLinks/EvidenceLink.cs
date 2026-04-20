using InfoFlowNavigator.Domain.Common;

namespace InfoFlowNavigator.Domain.EvidenceLinks;

public sealed record EvidenceLink(
    Guid Id,
    Guid EvidenceId,
    EvidenceLinkTargetKind TargetKind,
    Guid TargetId,
    EvidenceRelationToTarget RelationToTarget,
    EvidenceStrength Strength,
    string? Notes,
    double? Confidence,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    public static EvidenceLink Create(
        Guid evidenceId,
        EvidenceLinkTargetKind targetKind,
        Guid targetId,
        EvidenceRelationToTarget relationToTarget,
        EvidenceStrength strength = EvidenceStrength.Moderate,
        string? notes = null,
        double? confidence = null)
    {
        ValidateReferences(evidenceId, targetId);

        var now = DateTimeOffset.UtcNow;
        return new EvidenceLink(
            Guid.NewGuid(),
            evidenceId,
            targetKind,
            targetId,
            relationToTarget,
            strength,
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            DomainValidation.NormalizeConfidence(confidence, nameof(confidence)),
            now,
            now);
    }

    public EvidenceLink Update(
        EvidenceRelationToTarget relationToTarget,
        EvidenceStrength strength = EvidenceStrength.Moderate,
        string? notes = null,
        double? confidence = null)
    {
        ValidateReferences(EvidenceId, TargetId);

        return this with
        {
            RelationToTarget = relationToTarget,
            Strength = strength,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            Confidence = DomainValidation.NormalizeConfidence(confidence, nameof(confidence)),
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private static void ValidateReferences(Guid evidenceId, Guid targetId)
    {
        if (evidenceId == Guid.Empty)
        {
            throw new ArgumentException("Evidence id is required.", nameof(evidenceId));
        }

        if (targetId == Guid.Empty)
        {
            throw new ArgumentException("Target id is required.", nameof(targetId));
        }
    }
}
