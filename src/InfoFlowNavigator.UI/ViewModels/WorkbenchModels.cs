using InfoFlowNavigator.Application.Analysis;
using InfoFlowNavigator.Domain.Claims;
using InfoFlowNavigator.Domain.Events;
using InfoFlowNavigator.Domain.EvidenceLinks;
using InfoFlowNavigator.Domain.Hypotheses;

namespace InfoFlowNavigator.UI.ViewModels;

public enum WorkbenchSection
{
    Overview,
    Entities,
    Relationships,
    Events,
    Claims,
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

public sealed record RelationshipSummaryViewModel(
    Guid Id,
    Guid SourceEntityId,
    Guid TargetEntityId,
    string SourceName,
    string TargetName,
    string RelationshipType,
    string? Notes,
    double? Confidence)
{
    public string DisplayName => $"{SourceName} -> {RelationshipType} -> {TargetName}";
}

public sealed record EvidenceSummaryViewModel(Guid Id, string Title, string? Citation, string? Notes, double? Confidence)
{
    public string DisplayName => Title;
}

public sealed record EventSummaryViewModel(
    Guid Id,
    string Title,
    DateTimeOffset? OccurredAtUtc,
    string? Notes,
    double? Confidence,
    IReadOnlyList<EventParticipantRoleGroupViewModel> ParticipantRoleGroups)
{
    public string DisplayName => OccurredAtUtc is null ? Title : $"{OccurredAtUtc:yyyy-MM-dd}: {Title}";
    public string TimelineLabel => OccurredAtUtc?.ToString("yyyy-MM-dd HH:mm 'UTC'") ?? "Undated";
    public bool HasParticipants => ParticipantRoleGroups.Count > 0;
    public string ParticipantSummary => HasParticipants
        ? string.Join(" · ", ParticipantRoleGroups.Select(group => group.SummaryLabel))
        : "No linked entities yet.";
}

public sealed record EventParticipantRoleGroupViewModel(
    EventEntityLinkCategory Category,
    string Title,
    IReadOnlyList<string> Attendees)
{
    public string ShortTitle => Title.EndsWith("s", StringComparison.OrdinalIgnoreCase) ? Title[..^1] : Title;
    public int Count => Attendees.Count;
    public string CountLabel => $"{Title} ({Count})";
    public string SummaryLabel => $"{Count} {(Count == 1 ? ShortTitle : Title).ToLowerInvariant()}";
    public string AttendeeList => string.Join(", ", Attendees);
}

public sealed record EventParticipantSummaryViewModel(
    Guid Id,
    Guid EntityId,
    string EntityDisplayName,
    EventEntityLinkCategory Category,
    string? RoleDetail,
    double? Confidence,
    string? Notes)
{
    public string CategoryDisplayName => EventParticipant.GetCategoryDisplayName(Category);
    public string Role => string.IsNullOrWhiteSpace(RoleDetail) ? CategoryDisplayName : $"{CategoryDisplayName}: {RoleDetail}";
    public string DisplayName => $"{EntityDisplayName} · {Role}";
    public string SecondaryText => Confidence is null
        ? Notes ?? "No confidence"
        : $"Confidence {Confidence:0.##}{(string.IsNullOrWhiteSpace(Notes) ? string.Empty : $" | {Notes}")}";
}

public sealed record EventEntityLinkCategoryOptionViewModel(EventEntityLinkCategory Category, string DisplayName);

public sealed record ClaimSummaryViewModel(
    Guid Id,
    string Statement,
    ClaimType ClaimType,
    ClaimStatus Status,
    double? Confidence,
    string? Notes,
    ClaimTargetKind? TargetKind,
    Guid? TargetId,
    Guid? HypothesisId)
{
    public string DisplayName => $"{Statement} [{Status}]";
}

public sealed record ClaimTypeOptionViewModel(ClaimType ClaimType, string DisplayName);

public sealed record ClaimStatusOptionViewModel(ClaimStatus Status, string DisplayName);

public sealed record ClaimTargetKindOptionViewModel(ClaimTargetKind? Kind, string DisplayName);

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
    Guid EvidenceId,
    string EvidenceTitle,
    EvidenceLinkTargetKind TargetKind,
    Guid TargetId,
    string TargetDisplayName,
    EvidenceRelationToTarget RelationToTarget,
    EvidenceStrength Strength,
    string? Notes,
    double? Confidence)
{
    public string DisplayName => $"{EvidenceTitle} -> {TargetKind} -> {TargetDisplayName}";
    public string SecondaryText => $"{RelationToTarget} | {Strength}{(string.IsNullOrWhiteSpace(Notes) ? string.Empty : $" | {Notes}")}";
}

public sealed record FindingGroupViewModel(string Title, IReadOnlyList<AnalysisFinding> Items);
