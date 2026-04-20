using InfoFlowNavigator.Domain.EvidenceLinks;
using InfoFlowNavigator.Domain.Hypotheses;

namespace InfoFlowNavigator.UI.ViewModels;

public enum WorkbenchSection
{
    Overview,
    Entities,
    Relationships,
    Events,
    Hypotheses,
    Evidence,
    Findings
}

public sealed record WorkbenchSectionItemViewModel(
    WorkbenchSection Section,
    string Title,
    string Description,
    string ShortLabel);

public sealed record EntitySummaryViewModel(Guid Id, string Name, string EntityType, string? Notes, double? Confidence)
{
    public string DisplayName => $"{Name} ({EntityType})";
}

public sealed record EntityOptionViewModel(Guid Id, string DisplayName);

public sealed record RelationshipSummaryViewModel(Guid Id, string SourceName, string TargetName, string RelationshipType, string? Notes, double? Confidence)
{
    public string DisplayName => $"{SourceName} -> {RelationshipType} -> {TargetName}";
}

public sealed record EvidenceSummaryViewModel(Guid Id, string Title, string? Citation, string? Notes, double? Confidence)
{
    public string DisplayName => Title;
}

public sealed record EventSummaryViewModel(Guid Id, string Title, DateTimeOffset? OccurredAtUtc, string? Notes, double? Confidence)
{
    public string DisplayName => OccurredAtUtc is null ? Title : $"{OccurredAtUtc:yyyy-MM-dd}: {Title}";
    public string TimelineLabel => OccurredAtUtc?.ToString("yyyy-MM-dd HH:mm 'UTC'") ?? "Undated";
}

public sealed record HypothesisSummaryViewModel(Guid Id, string Title, string Statement, HypothesisStatus Status, double? Confidence, string? Notes)
{
    public string DisplayName => $"{Title} [{Status}]";
}

public sealed record HypothesisStatusOptionViewModel(HypothesisStatus Status, string DisplayName);

public sealed record LinkedEvidenceSummaryViewModel(
    Guid EvidenceLinkId,
    string Title,
    string? Citation,
    EvidenceRelationToTarget RelationToTarget,
    EvidenceStrength Strength,
    string? Notes,
    double? Confidence)
{
    public string DisplayName => $"{Title} ({RelationToTarget}, {Strength})";
    public string SecondaryText => string.IsNullOrWhiteSpace(Citation) ? Notes ?? "No citation" : Citation;
}

public sealed record TargetOptionViewModel(Guid Id, string DisplayName);

public sealed record EvidenceLinkTargetKindOptionViewModel(EvidenceLinkTargetKind Kind, string DisplayName);

public sealed record EvidenceRelationOptionViewModel(EvidenceRelationToTarget Relation, string DisplayName);

public sealed record EvidenceStrengthOptionViewModel(EvidenceStrength Strength, string DisplayName);

public sealed record EvidenceLinkSummaryViewModel(
    Guid Id,
    string EvidenceTitle,
    EvidenceLinkTargetKind TargetKind,
    string TargetDisplayName,
    EvidenceRelationToTarget RelationToTarget,
    EvidenceStrength Strength,
    string? Notes,
    double? Confidence)
{
    public string DisplayName => $"{EvidenceTitle} -> {TargetKind} -> {TargetDisplayName}";
    public string SecondaryText => $"{RelationToTarget} | {Strength}{(string.IsNullOrWhiteSpace(Notes) ? string.Empty : $" | {Notes}")}";
}
