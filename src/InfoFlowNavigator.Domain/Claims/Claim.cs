using InfoFlowNavigator.Domain.Common;

namespace InfoFlowNavigator.Domain.Claims;

public sealed record Claim(
    Guid Id,
    string Statement,
    ClaimType ClaimType,
    ClaimStatus Status,
    double? Confidence,
    string? Notes,
    IReadOnlyList<string> Tags,
    IReadOnlyDictionary<string, string> Metadata,
    ClaimTargetKind? TargetKind,
    Guid? TargetId,
    Guid? HypothesisId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    public static Claim Create(
        string statement,
        ClaimType claimType = ClaimType.General,
        ClaimStatus status = ClaimStatus.Draft,
        double? confidence = null,
        string? notes = null,
        IEnumerable<string>? tags = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        ClaimTargetKind? targetKind = null,
        Guid? targetId = null,
        Guid? hypothesisId = null)
    {
        ValidateTarget(targetKind, targetId);

        var now = DateTimeOffset.UtcNow;
        return new Claim(
            Guid.NewGuid(),
            DomainValidation.Required(statement, nameof(statement), "Claim statement is required."),
            claimType,
            status,
            DomainValidation.NormalizeConfidence(confidence, nameof(confidence)),
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            DomainValidation.NormalizeTags(tags),
            DomainValidation.NormalizeMetadata(metadata),
            targetKind,
            targetId,
            DomainValidation.NormalizeOptionalGuid(hypothesisId, nameof(hypothesisId)),
            now,
            now);
    }

    public Claim Update(
        string statement,
        ClaimType claimType,
        ClaimStatus status,
        double? confidence = null,
        string? notes = null,
        IEnumerable<string>? tags = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        ClaimTargetKind? targetKind = null,
        Guid? targetId = null,
        Guid? hypothesisId = null)
    {
        ValidateTarget(targetKind, targetId);

        return this with
        {
            Statement = DomainValidation.Required(statement, nameof(statement), "Claim statement is required."),
            ClaimType = claimType,
            Status = status,
            Confidence = DomainValidation.NormalizeConfidence(confidence, nameof(confidence)),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            Tags = DomainValidation.NormalizeTags(tags),
            Metadata = DomainValidation.NormalizeMetadata(metadata),
            TargetKind = targetKind,
            TargetId = targetId,
            HypothesisId = DomainValidation.NormalizeOptionalGuid(hypothesisId, nameof(hypothesisId)),
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private static void ValidateTarget(ClaimTargetKind? targetKind, Guid? targetId)
    {
        if (targetKind is null && targetId is null)
        {
            return;
        }

        if (targetKind is null || targetId is null)
        {
            throw new ArgumentException("Claim target kind and target id must both be provided.");
        }

        if (targetId == Guid.Empty)
        {
            throw new ArgumentException("Claim target id cannot be empty.", nameof(targetId));
        }
    }
}
