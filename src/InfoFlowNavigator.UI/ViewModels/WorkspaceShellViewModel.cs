using System.Collections.ObjectModel;
using System.Globalization;
using InfoFlowNavigator.Application.Abstractions;
using InfoFlowNavigator.Application.Analysis;
using InfoFlowNavigator.Application.Workspaces;
using InfoFlowNavigator.Domain.EvidenceLinks;
using InfoFlowNavigator.Domain.Workspaces;

namespace InfoFlowNavigator.UI.ViewModels;

public sealed class WorkspaceShellViewModel : ViewModelBase
{
    private readonly WorkspaceApplicationService _workspaceService;
    private readonly IAnalysisService _analysisService;
    private AnalysisWorkspace _workspace;
    private string _workspaceName = string.Empty;
    private string _workspacePath = "workspace.ifn.json";
    private string _statusMessage;
    private WorkbenchSectionItemViewModel? _selectedSection;

    public WorkspaceShellViewModel(WorkspaceApplicationService workspaceService, IAnalysisService analysisService)
    {
        _workspaceService = workspaceService;
        _analysisService = analysisService;
        _workspace = workspaceService.CreateWorkspace("Untitled Workspace");
        _statusMessage = "Create or open a workspace to begin.";

        Sections = new ObservableCollection<WorkbenchSectionItemViewModel>
        {
            new(WorkbenchSection.Overview, "Overview", "Case summary and guidance", "OV"),
            new(WorkbenchSection.Entities, "Entities", "People, organizations, and assets", "EN"),
            new(WorkbenchSection.Relationships, "Relationships", "How subjects are connected", "RE"),
            new(WorkbenchSection.Events, "Events", "Observed activity and chronology", "EV"),
            new(WorkbenchSection.Evidence, "Evidence", "Sources and support links", "ED"),
            new(WorkbenchSection.Findings, "Findings", "Explainable analysis guidance", "FI")
        };

        NewWorkspaceCommand = new RelayCommand(() => ExecuteSafely(CreateNewWorkspace));
        OpenWorkspaceCommand = new AsyncRelayCommand(() => ExecuteSafelyAsync(OpenWorkspaceAsync));
        SaveWorkspaceCommand = new AsyncRelayCommand(() => ExecuteSafelyAsync(SaveWorkspaceAsync));

        ShowOverviewCommand = new RelayCommand(() => SelectSection(WorkbenchSection.Overview));
        ShowEntitiesCommand = new RelayCommand(() => SelectSection(WorkbenchSection.Entities));
        ShowRelationshipsCommand = new RelayCommand(() => SelectSection(WorkbenchSection.Relationships));
        ShowEventsCommand = new RelayCommand(() => SelectSection(WorkbenchSection.Events));
        ShowEvidenceCommand = new RelayCommand(() => SelectSection(WorkbenchSection.Evidence));
        ShowFindingsCommand = new RelayCommand(() => SelectSection(WorkbenchSection.Findings));

        Overview = new OverviewViewModel();
        Entities = new EntitiesViewModel(
            new RelayCommand(() => ExecuteSafely(AddEntity)),
            new RelayCommand(() => ExecuteSafely(UpdateSelectedEntity)),
            new RelayCommand(() => ExecuteSafely(DeleteSelectedEntity)),
            _ => RefreshEntityLinkedEvidence());
        Relationships = new RelationshipsViewModel(
            new RelayCommand(() => ExecuteSafely(AddRelationship)),
            new RelayCommand(() => ExecuteSafely(DeleteSelectedRelationship)),
            _ => RefreshRelationshipLinkedEvidence());
        Events = new EventsViewModel(
            new RelayCommand(() => ExecuteSafely(SaveEvent)),
            new RelayCommand(() => ExecuteSafely(DeleteSelectedEvent)),
            _ => RefreshEventLinkedEvidence());
        Evidence = new EvidenceViewModel(
            new RelayCommand(() => ExecuteSafely(SaveEvidence)),
            new RelayCommand(() => ExecuteSafely(DeleteSelectedEvidence)),
            new RelayCommand(() => ExecuteSafely(AddEvidenceLink)),
            new RelayCommand(() => ExecuteSafely(DeleteSelectedEvidenceLink)),
            _ => RefreshEvidenceLinksForSelection(),
            kind => RefreshEvidenceTargets(kind));
        Findings = new FindingsViewModel();

        WorkspaceName = _workspace.Name;
        SelectedSection = Sections[0];
        RefreshWorkspaceState();
    }

    public string Title => "Info Flow Navigator";

    public string WorkspaceName
    {
        get => _workspaceName;
        set => SetProperty(ref _workspaceName, value);
    }

    public string WorkspacePath
    {
        get => _workspacePath;
        set => SetProperty(ref _workspacePath, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public AnalysisWorkspace Workspace
    {
        get => _workspace;
        private set
        {
            if (SetProperty(ref _workspace, value))
            {
                OnPropertyChanged(nameof(WorkspaceEntityCount));
                OnPropertyChanged(nameof(WorkspaceRelationshipCount));
                OnPropertyChanged(nameof(WorkspaceEventCount));
                OnPropertyChanged(nameof(WorkspaceEvidenceCount));
                OnPropertyChanged(nameof(WorkspaceEvidenceLinkCount));
                OnPropertyChanged(nameof(WorkspaceCreatedAt));
                OnPropertyChanged(nameof(WorkspaceUpdatedAt));
                OnPropertyChanged(nameof(WorkspaceId));
            }
        }
    }

    public int WorkspaceEntityCount => Workspace.Entities.Count;

    public int WorkspaceRelationshipCount => Workspace.Relationships.Count;

    public int WorkspaceEventCount => Workspace.Events.Count;

    public int WorkspaceEvidenceCount => Workspace.Evidence.Count;

    public int WorkspaceEvidenceLinkCount => Workspace.EvidenceLinks.Count;

    public string WorkspaceId => Workspace.Id.ToString();

    public string WorkspaceCreatedAt => Workspace.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");

    public string WorkspaceUpdatedAt => Workspace.UpdatedAtUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");

    public ObservableCollection<WorkbenchSectionItemViewModel> Sections { get; }

    public WorkbenchSectionItemViewModel? SelectedSection
    {
        get => _selectedSection;
        set
        {
            if (SetProperty(ref _selectedSection, value))
            {
                OnPropertyChanged(nameof(CurrentSectionTitle));
                OnPropertyChanged(nameof(CurrentSectionDescription));
                OnPropertyChanged(nameof(IsOverviewMode));
                OnPropertyChanged(nameof(IsEntitiesMode));
                OnPropertyChanged(nameof(IsRelationshipsMode));
                OnPropertyChanged(nameof(IsEventsMode));
                OnPropertyChanged(nameof(IsEvidenceMode));
                OnPropertyChanged(nameof(IsFindingsMode));
            }
        }
    }

    public string CurrentSectionTitle => SelectedSection?.Title ?? "Overview";

    public string CurrentSectionDescription => SelectedSection?.Description ?? "Case summary and guidance";

    public bool IsOverviewMode => SelectedSection?.Section == WorkbenchSection.Overview;

    public bool IsEntitiesMode => SelectedSection?.Section == WorkbenchSection.Entities;

    public bool IsRelationshipsMode => SelectedSection?.Section == WorkbenchSection.Relationships;

    public bool IsEventsMode => SelectedSection?.Section == WorkbenchSection.Events;

    public bool IsEvidenceMode => SelectedSection?.Section == WorkbenchSection.Evidence;

    public bool IsFindingsMode => SelectedSection?.Section == WorkbenchSection.Findings;

    public OverviewViewModel Overview { get; }

    public EntitiesViewModel Entities { get; }

    public RelationshipsViewModel Relationships { get; }

    public EventsViewModel Events { get; }

    public EvidenceViewModel Evidence { get; }

    public FindingsViewModel Findings { get; }

    public RelayCommand NewWorkspaceCommand { get; }

    public AsyncRelayCommand OpenWorkspaceCommand { get; }

    public AsyncRelayCommand SaveWorkspaceCommand { get; }

    public RelayCommand ShowOverviewCommand { get; }

    public RelayCommand ShowEntitiesCommand { get; }

    public RelayCommand ShowRelationshipsCommand { get; }

    public RelayCommand ShowEventsCommand { get; }

    public RelayCommand ShowEvidenceCommand { get; }

    public RelayCommand ShowFindingsCommand { get; }

    public void SetStatus(string message) => StatusMessage = message;

    private void SelectSection(WorkbenchSection section)
    {
        var selected = Sections.FirstOrDefault(item => item.Section == section);
        if (selected is not null)
        {
            SelectedSection = selected;
        }
    }

    private void CreateNewWorkspace()
    {
        var name = string.IsNullOrWhiteSpace(WorkspaceName) ? "Untitled Workspace" : WorkspaceName;
        SetWorkspace(_workspaceService.CreateWorkspace(name));
        StatusMessage = "Created a new workspace.";
    }

    private async Task OpenWorkspaceAsync()
    {
        EnsurePathProvided();
        SetWorkspace(await _workspaceService.OpenAsync(WorkspacePath.Trim()));
        StatusMessage = $"Opened workspace from '{WorkspacePath}'.";
    }

    private async Task SaveWorkspaceAsync()
    {
        EnsurePathProvided();
        SetWorkspace(_workspaceService.RenameWorkspace(Workspace, WorkspaceName));
        await _workspaceService.SaveAsync(WorkspacePath.Trim(), Workspace);
        StatusMessage = $"Saved workspace to '{WorkspacePath}'.";
    }

    private void AddEntity()
    {
        SetWorkspace(_workspaceService.AddEntity(Workspace, Entities.NewEntityName, Entities.NewEntityType));
        Entities.ClearAddForm();
        StatusMessage = "Added entity to the workspace.";
    }

    private void UpdateSelectedEntity()
    {
        if (Entities.SelectedEntity is null)
        {
            throw new InvalidOperationException("Select an entity to update.");
        }

        var confidence = ParseOptionalConfidence(Entities.EditorConfidenceText, "Entity confidence");
        SetWorkspace(_workspaceService.UpdateEntity(
            Workspace,
            Entities.SelectedEntity.Id,
            Entities.EditorName,
            Entities.EditorType,
            Entities.EditorNotes,
            confidence));
        StatusMessage = "Updated selected entity.";
    }

    private void DeleteSelectedEntity()
    {
        if (Entities.SelectedEntity is null)
        {
            throw new InvalidOperationException("Select an entity to delete.");
        }

        var name = Entities.SelectedEntity.Name;
        SetWorkspace(_workspaceService.RemoveEntity(Workspace, Entities.SelectedEntity.Id));
        StatusMessage = $"Deleted entity '{name}'.";
    }

    private void AddRelationship()
    {
        if (Relationships.SelectedSource is null || Relationships.SelectedTarget is null)
        {
            throw new InvalidOperationException("Select both source and target entities.");
        }

        SetWorkspace(_workspaceService.AddRelationship(
            Workspace,
            Relationships.SelectedSource.Id,
            Relationships.SelectedTarget.Id,
            Relationships.RelationshipType));
        StatusMessage = "Added relationship to the workspace.";
    }

    private void DeleteSelectedRelationship()
    {
        if (Relationships.SelectedRelationship is null)
        {
            throw new InvalidOperationException("Select a relationship to delete.");
        }

        SetWorkspace(_workspaceService.RemoveRelationship(Workspace, Relationships.SelectedRelationship.Id));
        StatusMessage = "Deleted selected relationship.";
    }

    private void SaveEvent()
    {
        var confidence = ParseOptionalConfidence(Events.EventConfidenceText, "Event confidence");
        var occurredAtUtc = ParseOptionalDateTimeOffset(Events.EventOccurredAtText, "Event occurred time");

        if (Events.SelectedEvent is null)
        {
            SetWorkspace(_workspaceService.AddEvent(Workspace, Events.EventTitle, occurredAtUtc, Events.EventNotes, confidence));
            StatusMessage = "Added event to the workspace.";
            return;
        }

        SetWorkspace(_workspaceService.UpdateEvent(
            Workspace,
            Events.SelectedEvent.Id,
            Events.EventTitle,
            occurredAtUtc,
            Events.EventNotes,
            confidence));
        StatusMessage = "Updated selected event.";
    }

    private void DeleteSelectedEvent()
    {
        if (Events.SelectedEvent is null)
        {
            throw new InvalidOperationException("Select an event to delete.");
        }

        var title = Events.SelectedEvent.Title;
        SetWorkspace(_workspaceService.RemoveEvent(Workspace, Events.SelectedEvent.Id));
        StatusMessage = $"Deleted event '{title}'.";
    }

    private void SaveEvidence()
    {
        var confidence = ParseOptionalConfidence(Evidence.EvidenceConfidenceText, "Evidence confidence");

        if (Evidence.SelectedEvidence is null)
        {
            SetWorkspace(_workspaceService.AddEvidence(Workspace, Evidence.EvidenceTitle, Evidence.EvidenceCitation, Evidence.EvidenceNotes, confidence));
            StatusMessage = "Added evidence to the workspace.";
            return;
        }

        SetWorkspace(_workspaceService.UpdateEvidence(
            Workspace,
            Evidence.SelectedEvidence.Id,
            Evidence.EvidenceTitle,
            Evidence.EvidenceCitation,
            Evidence.EvidenceNotes,
            confidence));
        StatusMessage = "Updated selected evidence.";
    }

    private void DeleteSelectedEvidence()
    {
        if (Evidence.SelectedEvidence is null)
        {
            throw new InvalidOperationException("Select evidence to delete.");
        }

        var title = Evidence.SelectedEvidence.Title;
        SetWorkspace(_workspaceService.RemoveEvidence(Workspace, Evidence.SelectedEvidence.Id));
        StatusMessage = $"Deleted evidence '{title}'.";
    }

    private void AddEvidenceLink()
    {
        if (Evidence.SelectedEvidence is null)
        {
            throw new InvalidOperationException("Select an evidence item first.");
        }

        if (Evidence.SelectedTargetKind is null || Evidence.SelectedTarget is null)
        {
            throw new InvalidOperationException("Select a target kind and target.");
        }

        var confidence = ParseOptionalConfidence(Evidence.LinkConfidenceText, "Evidence link confidence");
        SetWorkspace(_workspaceService.AddEvidenceLink(
            Workspace,
            Evidence.SelectedEvidence.Id,
            Evidence.SelectedTargetKind.Kind,
            Evidence.SelectedTarget.Id,
            Evidence.LinkRole,
            Evidence.LinkNotes,
            confidence));
        StatusMessage = "Added evidence link.";
    }

    private void DeleteSelectedEvidenceLink()
    {
        if (Evidence.SelectedLink is null)
        {
            throw new InvalidOperationException("Select an evidence link to delete.");
        }

        SetWorkspace(_workspaceService.RemoveEvidenceLink(Workspace, Evidence.SelectedLink.Id));
        StatusMessage = "Deleted selected evidence link.";
    }

    private void SetWorkspace(AnalysisWorkspace workspace)
    {
        Workspace = workspace;
        WorkspaceName = workspace.Name;
        RefreshWorkspaceState();
    }

    private void RefreshWorkspaceState()
    {
        var selectedEntityId = Entities.SelectedEntity?.Id;
        var selectedRelationshipId = Relationships.SelectedRelationship?.Id;
        var selectedEventId = Events.SelectedEvent?.Id;
        var selectedEvidenceId = Evidence.SelectedEvidence?.Id;
        var selectedLinkId = Evidence.SelectedLink?.Id;
        var selectedSourceId = Relationships.SelectedSource?.Id;
        var selectedTargetId = Relationships.SelectedTarget?.Id;
        var targetKind = Evidence.SelectedTargetKind?.Kind ?? EvidenceLinkTargetKind.Entity;

        var analysis = _analysisService.SummarizeAsync(Workspace).GetAwaiter().GetResult();

        Overview.Refresh(Workspace.Name, analysis);
        Findings.Refresh(analysis);

        var entityItems = Workspace.Entities
            .OrderBy(entity => entity.Name, StringComparer.OrdinalIgnoreCase)
            .Select(entity => new EntitySummaryViewModel(entity.Id, entity.Name, entity.EntityType, entity.Notes, entity.Confidence))
            .ToArray();

        var entityOptions = Workspace.Entities
            .OrderBy(entity => entity.Name, StringComparer.OrdinalIgnoreCase)
            .Select(entity => new EntityOptionViewModel(entity.Id, $"{entity.Name} ({entity.EntityType})"))
            .ToArray();

        var relationshipItems = Workspace.Relationships
            .Select(relationship => new RelationshipSummaryViewModel(
                relationship.Id,
                ResolveEntityName(relationship.SourceEntityId),
                ResolveEntityName(relationship.TargetEntityId),
                relationship.RelationshipType,
                relationship.Notes,
                relationship.Confidence))
            .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var eventItems = Workspace.Events
            .Select(@event => new EventSummaryViewModel(@event.Id, @event.Title, @event.OccurredAtUtc, @event.Notes, @event.Confidence))
            .ToArray();

        var evidenceItems = Workspace.Evidence
            .OrderBy(evidence => evidence.Title, StringComparer.OrdinalIgnoreCase)
            .Select(evidence => new EvidenceSummaryViewModel(evidence.Id, evidence.Title, evidence.Citation, evidence.Notes, evidence.Confidence))
            .ToArray();

        Entities.Refresh(entityItems, selectedEntityId);
        Relationships.Refresh(relationshipItems, entityOptions, selectedRelationshipId, selectedSourceId, selectedTargetId);
        Events.Refresh(eventItems, selectedEventId);
        Evidence.Refresh(
            evidenceItems,
            selectedEvidenceId,
            BuildEvidenceLinkSummaries(selectedEvidenceId),
            CreateTargetKindOptions(),
            BuildTargetOptions(targetKind),
            selectedLinkId);

        RefreshEntityLinkedEvidence();
        RefreshRelationshipLinkedEvidence();
        RefreshEventLinkedEvidence();
        RefreshEvidenceLinksForSelection();
        RefreshEvidenceTargets(targetKind);

        OnPropertyChanged(nameof(WorkspaceEntityCount));
        OnPropertyChanged(nameof(WorkspaceRelationshipCount));
        OnPropertyChanged(nameof(WorkspaceEventCount));
        OnPropertyChanged(nameof(WorkspaceEvidenceCount));
        OnPropertyChanged(nameof(WorkspaceEvidenceLinkCount));
        OnPropertyChanged(nameof(WorkspaceCreatedAt));
        OnPropertyChanged(nameof(WorkspaceUpdatedAt));
        OnPropertyChanged(nameof(WorkspaceId));
    }

    private void RefreshEntityLinkedEvidence()
    {
        var linkedEvidence = Entities.SelectedEntity is null
            ? []
            : BuildLinkedEvidence(_workspaceService.GetLinkedEvidenceByTarget(Workspace, EvidenceLinkTargetKind.Entity, Entities.SelectedEntity.Id));

        Entities.UpdateLinkedEvidence(linkedEvidence);
    }

    private void RefreshRelationshipLinkedEvidence()
    {
        var linkedEvidence = Relationships.SelectedRelationship is null
            ? []
            : BuildLinkedEvidence(_workspaceService.GetLinkedEvidenceByTarget(Workspace, EvidenceLinkTargetKind.Relationship, Relationships.SelectedRelationship.Id));

        Relationships.UpdateLinkedEvidence(linkedEvidence);
    }

    private void RefreshEventLinkedEvidence()
    {
        var linkedEvidence = Events.SelectedEvent is null
            ? []
            : BuildLinkedEvidence(_workspaceService.GetLinkedEvidenceByTarget(Workspace, EvidenceLinkTargetKind.Event, Events.SelectedEvent.Id));

        Events.UpdateLinkedEvidence(linkedEvidence);
    }

    private void RefreshEvidenceLinksForSelection()
    {
        Evidence.UpdateLinkedTargets(BuildEvidenceLinkSummaries(Evidence.SelectedEvidence?.Id));
    }

    private void RefreshEvidenceTargets(EvidenceLinkTargetKind targetKind) =>
        Evidence.UpdateTargets(BuildTargetOptions(targetKind));

    private IReadOnlyList<LinkedEvidenceSummaryViewModel> BuildLinkedEvidence(IReadOnlyList<LinkedEvidenceSummary> linkedEvidence) =>
        linkedEvidence
            .Select(item => new LinkedEvidenceSummaryViewModel(item.EvidenceLinkId, item.Title, item.Citation, item.Role, item.LinkNotes, item.LinkConfidence))
            .ToArray();

    private IReadOnlyList<EvidenceLinkSummaryViewModel> BuildEvidenceLinkSummaries(Guid? evidenceId)
    {
        if (evidenceId is null)
        {
            return [];
        }

        return Workspace.EvidenceLinks
            .Where(link => link.EvidenceId == evidenceId)
            .OrderBy(link => link.TargetKind)
            .ThenBy(link => link.TargetId)
            .Select(link => new EvidenceLinkSummaryViewModel(
                link.Id,
                Workspace.Evidence.First(evidence => evidence.Id == link.EvidenceId).Title,
                link.TargetKind,
                ResolveTargetDisplay(link.TargetKind, link.TargetId),
                link.Role,
                link.Notes,
                link.Confidence))
            .ToArray();
    }

    private IReadOnlyList<TargetOptionViewModel> BuildTargetOptions(EvidenceLinkTargetKind targetKind) =>
        targetKind switch
        {
            EvidenceLinkTargetKind.Entity => Workspace.Entities
                .OrderBy(entity => entity.Name, StringComparer.OrdinalIgnoreCase)
                .Select(entity => new TargetOptionViewModel(entity.Id, $"{entity.Name} ({entity.EntityType})"))
                .ToArray(),
            EvidenceLinkTargetKind.Relationship => Workspace.Relationships
                .Select(relationship => new TargetOptionViewModel(
                    relationship.Id,
                    $"{ResolveEntityName(relationship.SourceEntityId)} -> {relationship.RelationshipType} -> {ResolveEntityName(relationship.TargetEntityId)}"))
                .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            EvidenceLinkTargetKind.Event => Workspace.Events
                .OrderBy(@event => @event.OccurredAtUtc ?? DateTimeOffset.MaxValue)
                .ThenBy(@event => @event.Title, StringComparer.OrdinalIgnoreCase)
                .Select(@event => new TargetOptionViewModel(@event.Id, @event.Title))
                .ToArray(),
            _ => []
        };

    private static IReadOnlyList<EvidenceLinkTargetKindOptionViewModel> CreateTargetKindOptions() =>
    [
        new(EvidenceLinkTargetKind.Entity, "Entity"),
        new(EvidenceLinkTargetKind.Relationship, "Relationship"),
        new(EvidenceLinkTargetKind.Event, "Event")
    ];

    private string ResolveEntityName(Guid entityId) =>
        Workspace.Entities.FirstOrDefault(entity => entity.Id == entityId)?.Name ?? entityId.ToString();

    private string ResolveTargetDisplay(EvidenceLinkTargetKind targetKind, Guid targetId) =>
        targetKind switch
        {
            EvidenceLinkTargetKind.Entity => Workspace.Entities.FirstOrDefault(entity => entity.Id == targetId)?.DisplayName() ?? targetId.ToString(),
            EvidenceLinkTargetKind.Relationship => Workspace.Relationships
                .Where(relationship => relationship.Id == targetId)
                .Select(relationship => $"{ResolveEntityName(relationship.SourceEntityId)} -> {relationship.RelationshipType} -> {ResolveEntityName(relationship.TargetEntityId)}")
                .FirstOrDefault() ?? targetId.ToString(),
            EvidenceLinkTargetKind.Event => Workspace.Events.FirstOrDefault(@event => @event.Id == targetId)?.Title ?? targetId.ToString(),
            _ => targetId.ToString()
        };

    private static double? ParseOptionalConfidence(string text, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (!double.TryParse(text.Trim(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var value))
        {
            throw new InvalidOperationException($"{fieldName} must be a valid number between 0.0 and 1.0.");
        }

        return value;
    }

    private static DateTimeOffset? ParseOptionalDateTimeOffset(string text, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (!DateTimeOffset.TryParse(text.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowWhiteSpaces, out var value))
        {
            throw new InvalidOperationException($"{fieldName} must be a valid date/time.");
        }

        return value.ToUniversalTime();
    }

    private void EnsurePathProvided()
    {
        if (string.IsNullOrWhiteSpace(WorkspacePath))
        {
            throw new InvalidOperationException("Workspace path is required.");
        }
    }

    private void ExecuteSafely(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    private async Task ExecuteSafelyAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }
}

file static class EntityDisplayExtensions
{
    public static string DisplayName(this Domain.Entities.Entity entity) => $"{entity.Name} ({entity.EntityType})";
}
