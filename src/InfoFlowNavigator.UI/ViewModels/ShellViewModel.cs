using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using InfoFlowNavigator.Application.Abstractions;
using InfoFlowNavigator.Application.Analysis;
using InfoFlowNavigator.Application.Workspaces;
using InfoFlowNavigator.Domain.EvidenceLinks;
using InfoFlowNavigator.Domain.Workspaces;

namespace InfoFlowNavigator.UI.ViewModels;

public sealed class ShellViewModel : INotifyPropertyChanged
{
    private readonly WorkspaceApplicationService _workspaceService;
    private readonly IAnalysisService _analysisService;
    private AnalysisWorkspace _workspace;
    private string _workspaceName = string.Empty;
    private string _workspacePath = "workspace.ifn.json";
    private string _entityName = string.Empty;
    private string _entityType = "Person";
    private string _relationshipType = "associated_with";
    private EntityOptionViewModel? _selectedSourceEntity;
    private EntityOptionViewModel? _selectedTargetEntity;
    private EntitySummaryViewModel? _selectedEntity;
    private string _editableEntityName = string.Empty;
    private string _editableEntityType = string.Empty;
    private string _editableEntityNotes = string.Empty;
    private string _editableEntityConfidenceText = string.Empty;
    private RelationshipSummaryViewModel? _selectedRelationship;
    private EvidenceSummaryViewModel? _selectedEvidence;
    private string _evidenceTitle = string.Empty;
    private string _evidenceCitation = string.Empty;
    private string _evidenceNotes = string.Empty;
    private string _evidenceConfidenceText = string.Empty;
    private string _entityTypeSummary = "No entities to analyze yet.";
    private string _orphanEntitySummary = "No entities to analyze yet.";
    private string _topConnectedEntitySummary = "No relationships yet.";
    private string _missingConfidenceRelationshipSummary = "All relationship confidence gaps will appear here.";
    private string _evidenceSummaryText = "No evidence summary available yet.";
    private string _supportCoverageSummary = "No support coverage available yet.";
    private string _statusMessage;

    public ShellViewModel(WorkspaceApplicationService workspaceService, IAnalysisService analysisService)
    {
        _workspaceService = workspaceService;
        _analysisService = analysisService;
        _workspace = workspaceService.CreateWorkspace("Untitled Workspace");
        _statusMessage = "Create or open a workspace to begin.";

        Entities = new ObservableCollection<EntitySummaryViewModel>();
        Relationships = new ObservableCollection<RelationshipSummaryViewModel>();
        EvidenceItems = new ObservableCollection<EvidenceSummaryViewModel>();
        RelationshipEntityOptions = new ObservableCollection<EntityOptionViewModel>();
        Findings = new ObservableCollection<AnalysisFinding>();
        SelectedEntityLinkedEvidence = new ObservableCollection<LinkedEvidenceSummaryViewModel>();
        SelectedRelationshipLinkedEvidence = new ObservableCollection<LinkedEvidenceSummaryViewModel>();

        EventPanel = new EventPanelViewModel();
        EvidenceLinkPanel = new EvidenceLinkPanelViewModel();
        EventPanel.PropertyChanged += EventPanelOnPropertyChanged;

        WorkspaceName = _workspace.Name;
        RefreshWorkspaceState();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Title => "Info Flow Navigator";

    public string Subtitle => "Events and evidence-link slice";

    public string WorkspaceName
    {
        get => _workspaceName;
        set => SetField(ref _workspaceName, value);
    }

    public string WorkspacePath
    {
        get => _workspacePath;
        set => SetField(ref _workspacePath, value);
    }

    public string EntityName
    {
        get => _entityName;
        set => SetField(ref _entityName, value);
    }

    public string EntityType
    {
        get => _entityType;
        set => SetField(ref _entityType, value);
    }

    public string RelationshipType
    {
        get => _relationshipType;
        set => SetField(ref _relationshipType, value);
    }

    public EntityOptionViewModel? SelectedSourceEntity
    {
        get => _selectedSourceEntity;
        set => SetField(ref _selectedSourceEntity, value);
    }

    public EntityOptionViewModel? SelectedTargetEntity
    {
        get => _selectedTargetEntity;
        set => SetField(ref _selectedTargetEntity, value);
    }

    public EntitySummaryViewModel? SelectedEntity
    {
        get => _selectedEntity;
        set
        {
            if (SetField(ref _selectedEntity, value))
            {
                PopulateEntityEditor(value);
                RefreshSelectedEntityLinkedEvidence();
            }
        }
    }

    public string EditableEntityName
    {
        get => _editableEntityName;
        set => SetField(ref _editableEntityName, value);
    }

    public string EditableEntityType
    {
        get => _editableEntityType;
        set => SetField(ref _editableEntityType, value);
    }

    public string EditableEntityNotes
    {
        get => _editableEntityNotes;
        set => SetField(ref _editableEntityNotes, value);
    }

    public string EditableEntityConfidenceText
    {
        get => _editableEntityConfidenceText;
        set => SetField(ref _editableEntityConfidenceText, value);
    }

    public RelationshipSummaryViewModel? SelectedRelationship
    {
        get => _selectedRelationship;
        set
        {
            if (SetField(ref _selectedRelationship, value))
            {
                RefreshSelectedRelationshipLinkedEvidence();
            }
        }
    }

    public EvidenceSummaryViewModel? SelectedEvidence
    {
        get => _selectedEvidence;
        set
        {
            if (SetField(ref _selectedEvidence, value))
            {
                PopulateEvidenceEditor(value);
                OnPropertyChanged(nameof(EvidenceActionLabel));
            }
        }
    }

    public string EvidenceTitle
    {
        get => _evidenceTitle;
        set => SetField(ref _evidenceTitle, value);
    }

    public string EvidenceCitation
    {
        get => _evidenceCitation;
        set => SetField(ref _evidenceCitation, value);
    }

    public string EvidenceNotes
    {
        get => _evidenceNotes;
        set => SetField(ref _evidenceNotes, value);
    }

    public string EvidenceConfidenceText
    {
        get => _evidenceConfidenceText;
        set => SetField(ref _evidenceConfidenceText, value);
    }

    public string EvidenceActionLabel => SelectedEvidence is null ? "Add Evidence" : "Update Evidence";

    public string EntityTypeSummary
    {
        get => _entityTypeSummary;
        private set => SetField(ref _entityTypeSummary, value);
    }

    public string OrphanEntitySummary
    {
        get => _orphanEntitySummary;
        private set => SetField(ref _orphanEntitySummary, value);
    }

    public string TopConnectedEntitySummary
    {
        get => _topConnectedEntitySummary;
        private set => SetField(ref _topConnectedEntitySummary, value);
    }

    public string MissingConfidenceRelationshipSummary
    {
        get => _missingConfidenceRelationshipSummary;
        private set => SetField(ref _missingConfidenceRelationshipSummary, value);
    }

    public string EvidenceSummaryText
    {
        get => _evidenceSummaryText;
        private set => SetField(ref _evidenceSummaryText, value);
    }

    public string SupportCoverageSummary
    {
        get => _supportCoverageSummary;
        private set => SetField(ref _supportCoverageSummary, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public AnalysisWorkspace Workspace
    {
        get => _workspace;
        private set
        {
            if (SetField(ref _workspace, value))
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

    public ObservableCollection<EntitySummaryViewModel> Entities { get; }

    public ObservableCollection<RelationshipSummaryViewModel> Relationships { get; }

    public ObservableCollection<EvidenceSummaryViewModel> EvidenceItems { get; }

    public ObservableCollection<EntityOptionViewModel> RelationshipEntityOptions { get; }

    public ObservableCollection<AnalysisFinding> Findings { get; }

    public ObservableCollection<LinkedEvidenceSummaryViewModel> SelectedEntityLinkedEvidence { get; }

    public ObservableCollection<LinkedEvidenceSummaryViewModel> SelectedRelationshipLinkedEvidence { get; }

    public EventPanelViewModel EventPanel { get; }

    public EvidenceLinkPanelViewModel EvidenceLinkPanel { get; }

    public void CreateNewWorkspace()
    {
        var name = string.IsNullOrWhiteSpace(WorkspaceName) ? "Untitled Workspace" : WorkspaceName;
        SetWorkspace(_workspaceService.CreateWorkspace(name));
        StatusMessage = "Created a new workspace.";
    }

    public async Task OpenWorkspaceAsync(CancellationToken cancellationToken = default)
    {
        EnsurePathProvided();
        SetWorkspace(await _workspaceService.OpenAsync(WorkspacePath.Trim(), cancellationToken));
        StatusMessage = $"Opened workspace from '{WorkspacePath}'.";
    }

    public async Task SaveWorkspaceAsync(CancellationToken cancellationToken = default)
    {
        EnsurePathProvided();

        var renamed = _workspaceService.RenameWorkspace(Workspace, WorkspaceName);
        SetWorkspace(renamed);
        await _workspaceService.SaveAsync(WorkspacePath.Trim(), Workspace, cancellationToken);
        StatusMessage = $"Saved workspace to '{WorkspacePath}'.";
    }

    public void AddEntity()
    {
        SetWorkspace(_workspaceService.AddEntity(Workspace, EntityName, EntityType));
        EntityName = string.Empty;
        StatusMessage = "Added entity to the workspace.";
    }

    public void UpdateSelectedEntity()
    {
        if (SelectedEntity is null)
        {
            throw new InvalidOperationException("Select an entity to update.");
        }

        var confidence = ParseOptionalConfidence(EditableEntityConfidenceText, "Entity confidence");
        SetWorkspace(_workspaceService.UpdateEntity(Workspace, SelectedEntity.Id, EditableEntityName, EditableEntityType, EditableEntityNotes, confidence));
        StatusMessage = "Updated selected entity.";
    }

    public void DeleteSelectedEntity()
    {
        if (SelectedEntity is null)
        {
            throw new InvalidOperationException("Select an entity to delete.");
        }

        var deletedEntityName = SelectedEntity.Name;
        SetWorkspace(_workspaceService.RemoveEntity(Workspace, SelectedEntity.Id));
        StatusMessage = $"Deleted entity '{deletedEntityName}'.";
    }

    public void AddRelationship()
    {
        if (SelectedSourceEntity is null || SelectedTargetEntity is null)
        {
            throw new InvalidOperationException("Select both source and target entities.");
        }

        SetWorkspace(_workspaceService.AddRelationship(Workspace, SelectedSourceEntity.Id, SelectedTargetEntity.Id, RelationshipType));
        StatusMessage = "Added relationship to the workspace.";
    }

    public void DeleteSelectedRelationship()
    {
        if (SelectedRelationship is null)
        {
            throw new InvalidOperationException("Select a relationship to delete.");
        }

        SetWorkspace(_workspaceService.RemoveRelationship(Workspace, SelectedRelationship.Id));
        StatusMessage = "Deleted selected relationship.";
    }

    public void SaveEvidence()
    {
        var confidence = ParseOptionalConfidence(EvidenceConfidenceText, "Evidence confidence");

        if (SelectedEvidence is null)
        {
            SetWorkspace(_workspaceService.AddEvidence(Workspace, EvidenceTitle, EvidenceCitation, EvidenceNotes, confidence));
            StatusMessage = "Added evidence to the workspace.";
            return;
        }

        SetWorkspace(_workspaceService.UpdateEvidence(Workspace, SelectedEvidence.Id, EvidenceTitle, EvidenceCitation, EvidenceNotes, confidence));
        StatusMessage = "Updated selected evidence.";
    }

    public void DeleteSelectedEvidence()
    {
        if (SelectedEvidence is null)
        {
            throw new InvalidOperationException("Select evidence to delete.");
        }

        var deletedEvidenceTitle = SelectedEvidence.Title;
        SetWorkspace(_workspaceService.RemoveEvidence(Workspace, SelectedEvidence.Id));
        StatusMessage = $"Deleted evidence '{deletedEvidenceTitle}'.";
    }

    public void SaveEvent()
    {
        var confidence = ParseOptionalConfidence(EventPanel.EventConfidenceText, "Event confidence");
        var occurredAtUtc = ParseOptionalDateTimeOffset(EventPanel.EventOccurredAtText, "Event occurred time");

        if (EventPanel.SelectedEvent is null)
        {
            SetWorkspace(_workspaceService.AddEvent(Workspace, EventPanel.EventTitle, occurredAtUtc, EventPanel.EventNotes, confidence));
            StatusMessage = "Added event to the workspace.";
            return;
        }

        SetWorkspace(_workspaceService.UpdateEvent(Workspace, EventPanel.SelectedEvent.Id, EventPanel.EventTitle, occurredAtUtc, EventPanel.EventNotes, confidence));
        StatusMessage = "Updated selected event.";
    }

    public void DeleteSelectedEvent()
    {
        if (EventPanel.SelectedEvent is null)
        {
            throw new InvalidOperationException("Select an event to delete.");
        }

        var deletedTitle = EventPanel.SelectedEvent.Title;
        SetWorkspace(_workspaceService.RemoveEvent(Workspace, EventPanel.SelectedEvent.Id));
        StatusMessage = $"Deleted event '{deletedTitle}'.";
    }

    public void AddEvidenceLink()
    {
        if (EvidenceLinkPanel.SelectedEvidence is null)
        {
            throw new InvalidOperationException("Select evidence to link.");
        }

        if (EvidenceLinkPanel.SelectedTargetKindOption is null)
        {
            throw new InvalidOperationException("Select a target kind.");
        }

        if (EvidenceLinkPanel.SelectedTarget is null)
        {
            throw new InvalidOperationException("Select a target to link.");
        }

        var confidence = ParseOptionalConfidence(EvidenceLinkPanel.LinkConfidenceText, "Evidence link confidence");
        SetWorkspace(_workspaceService.AddEvidenceLink(
            Workspace,
            EvidenceLinkPanel.SelectedEvidence.Id,
            EvidenceLinkPanel.SelectedTargetKindOption.Kind,
            EvidenceLinkPanel.SelectedTarget.Id,
            EvidenceLinkPanel.LinkRole,
            EvidenceLinkPanel.LinkNotes,
            confidence));
        StatusMessage = "Added evidence link.";
    }

    public void DeleteSelectedEvidenceLink()
    {
        if (EvidenceLinkPanel.SelectedLink is null)
        {
            throw new InvalidOperationException("Select an evidence link to delete.");
        }

        SetWorkspace(_workspaceService.RemoveEvidenceLink(Workspace, EvidenceLinkPanel.SelectedLink.Id));
        StatusMessage = "Deleted selected evidence link.";
    }

    public void SetStatus(string message) => StatusMessage = message;

    private void SetWorkspace(AnalysisWorkspace workspace)
    {
        Workspace = workspace;
        WorkspaceName = workspace.Name;
        RefreshWorkspaceState();
    }

    private void RefreshWorkspaceState()
    {
        var selectedEntityId = SelectedEntity?.Id;
        var selectedRelationshipId = SelectedRelationship?.Id;
        var selectedEvidenceId = SelectedEvidence?.Id;
        var selectedSourceEntityId = SelectedSourceEntity?.Id;
        var selectedTargetEntityId = SelectedTargetEntity?.Id;
        var selectedEventId = EventPanel.SelectedEvent?.Id;
        var selectedLinkId = EvidenceLinkPanel.SelectedLink?.Id;

        Entities.Clear();
        foreach (var entity in Workspace.Entities)
        {
            Entities.Add(new EntitySummaryViewModel(entity.Id, entity.Name, entity.EntityType, entity.Notes, entity.Confidence));
        }

        RelationshipEntityOptions.Clear();
        foreach (var entity in Workspace.Entities)
        {
            RelationshipEntityOptions.Add(new EntityOptionViewModel(entity.Id, $"{entity.Name} ({entity.EntityType})"));
        }

        Relationships.Clear();
        foreach (var relationship in Workspace.Relationships)
        {
            Relationships.Add(new RelationshipSummaryViewModel(
                relationship.Id,
                ResolveEntityName(relationship.SourceEntityId),
                ResolveEntityName(relationship.TargetEntityId),
                relationship.RelationshipType,
                relationship.Notes,
                relationship.Confidence));
        }

        EvidenceItems.Clear();
        foreach (var evidence in Workspace.Evidence)
        {
            EvidenceItems.Add(new EvidenceSummaryViewModel(evidence.Id, evidence.Title, evidence.Citation, evidence.Notes, evidence.Confidence));
        }

        EventPanel.Refresh(
            Workspace.Events.Select(@event => new EventSummaryViewModel(@event.Id, @event.Title, @event.OccurredAtUtc, @event.Notes, @event.Confidence)).ToArray(),
            selectedEventId,
            eventId => eventId is null
                ? []
                : BuildLinkedEvidenceViewModels(_workspaceService.GetLinkedEvidenceByTarget(Workspace, EvidenceLinkTargetKind.Event, eventId.Value)));

        EvidenceLinkPanel.Refresh(
            Workspace.Evidence.Select(evidence => new EvidenceOptionViewModel(evidence.Id, evidence.Title)).ToArray(),
            CreateTargetKindOptions(),
            BuildTargetOptions(EvidenceLinkTargetKind.Entity),
            Workspace.EvidenceLinks.Select(BuildEvidenceLinkSummary).ToArray(),
            selectedLinkId);
        EvidenceLinkPanel.SetTargetResolver(BuildTargetOptions);

        SelectedEntity = selectedEntityId is null ? null : Entities.FirstOrDefault(entity => entity.Id == selectedEntityId);
        SelectedRelationship = selectedRelationshipId is null ? null : Relationships.FirstOrDefault(relationship => relationship.Id == selectedRelationshipId);
        SelectedEvidence = selectedEvidenceId is null ? null : EvidenceItems.FirstOrDefault(evidence => evidence.Id == selectedEvidenceId);

        if (RelationshipEntityOptions.Count > 0)
        {
            SelectedSourceEntity = selectedSourceEntityId is null
                ? RelationshipEntityOptions[0]
                : RelationshipEntityOptions.FirstOrDefault(entity => entity.Id == selectedSourceEntityId) ?? RelationshipEntityOptions[0];

            SelectedTargetEntity = selectedTargetEntityId is null
                ? RelationshipEntityOptions[0]
                : RelationshipEntityOptions.FirstOrDefault(entity => entity.Id == selectedTargetEntityId) ?? RelationshipEntityOptions[0];
        }
        else
        {
            SelectedSourceEntity = null;
            SelectedTargetEntity = null;
        }

        if (SelectedEntity is null)
        {
            ClearEntityEditor();
        }

        if (SelectedEvidence is null)
        {
            ClearEvidenceEditor();
        }

        RefreshSelectedEntityLinkedEvidence();
        RefreshSelectedRelationshipLinkedEvidence();
        RefreshAnalysisState();

        OnPropertyChanged(nameof(WorkspaceEntityCount));
        OnPropertyChanged(nameof(WorkspaceRelationshipCount));
        OnPropertyChanged(nameof(WorkspaceEventCount));
        OnPropertyChanged(nameof(WorkspaceEvidenceCount));
        OnPropertyChanged(nameof(WorkspaceEvidenceLinkCount));
        OnPropertyChanged(nameof(WorkspaceCreatedAt));
        OnPropertyChanged(nameof(WorkspaceUpdatedAt));
        OnPropertyChanged(nameof(WorkspaceId));
        OnPropertyChanged(nameof(EvidenceActionLabel));
    }

    private void RefreshAnalysisState()
    {
        var analysis = _analysisService.SummarizeAsync(Workspace).GetAwaiter().GetResult();

        Findings.Clear();
        foreach (var finding in analysis.Findings)
        {
            Findings.Add(finding);
        }

        EntityTypeSummary = analysis.EntityCountByType.Count == 0
            ? "No entity types available."
            : string.Join(", ", analysis.EntityCountByType.Select(item => $"{item.EntityType} ({item.Count})"));

        OrphanEntitySummary = analysis.OrphanEntities.Count == 0
            ? "No orphan entities."
            : string.Join(", ", analysis.OrphanEntities.Select(entity => $"{entity.Name} [{entity.EntityType}]"));

        TopConnectedEntitySummary = analysis.TopConnectedEntities.Count == 0
            ? "No connected entities yet."
            : string.Join(", ", analysis.TopConnectedEntities.Select(entity => $"{entity.Name} ({entity.Degree})"));

        MissingConfidenceRelationshipSummary = analysis.RelationshipsMissingConfidence.Count == 0
            ? "All relationships have confidence values."
            : string.Join(", ", analysis.RelationshipsMissingConfidence.Select(relationship => $"{relationship.SourceEntityName} -> {relationship.RelationshipType} -> {relationship.TargetEntityName}"));

        EvidenceSummaryText =
            $"Total: {analysis.EvidenceSummary.TotalCount}; " +
            $"with citations: {analysis.EvidenceSummary.WithCitationCount}; " +
            $"missing citations: {analysis.EvidenceSummary.MissingCitationCount}; " +
            $"with confidence: {analysis.EvidenceSummary.WithConfidenceCount}; " +
            $"missing confidence: {analysis.EvidenceSummary.MissingConfidenceCount}.";

        SupportCoverageSummary =
            $"Unsupported relationships: {analysis.RelationshipsWithoutSupportingEvidence.Count}; " +
            $"unsupported events: {analysis.EventsWithoutSupportingEvidence.Count}; " +
            $"activity without events: {analysis.EntitiesWithActivityButNoEvents.Count}; " +
            $"chronology gaps: {analysis.ChronologyGaps.Count}.";
    }

    private void RefreshSelectedEntityLinkedEvidence()
    {
        ReplaceCollection(
            SelectedEntityLinkedEvidence,
            SelectedEntity is null
                ? []
                : BuildLinkedEvidenceViewModels(_workspaceService.GetLinkedEvidenceByTarget(Workspace, EvidenceLinkTargetKind.Entity, SelectedEntity.Id)));
    }

    private void RefreshSelectedRelationshipLinkedEvidence()
    {
        ReplaceCollection(
            SelectedRelationshipLinkedEvidence,
            SelectedRelationship is null
                ? []
                : BuildLinkedEvidenceViewModels(_workspaceService.GetLinkedEvidenceByTarget(Workspace, EvidenceLinkTargetKind.Relationship, SelectedRelationship.Id)));
    }

    private void PopulateEntityEditor(EntitySummaryViewModel? entity)
    {
        if (entity is null)
        {
            ClearEntityEditor();
            return;
        }

        var domainEntity = Workspace.Entities.First(existing => existing.Id == entity.Id);
        EditableEntityName = domainEntity.Name;
        EditableEntityType = domainEntity.EntityType;
        EditableEntityNotes = domainEntity.Notes ?? string.Empty;
        EditableEntityConfidenceText = domainEntity.Confidence?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private void PopulateEvidenceEditor(EvidenceSummaryViewModel? evidence)
    {
        if (evidence is null)
        {
            ClearEvidenceEditor();
            return;
        }

        var domainEvidence = Workspace.Evidence.First(existing => existing.Id == evidence.Id);
        EvidenceTitle = domainEvidence.Title;
        EvidenceCitation = domainEvidence.Citation ?? string.Empty;
        EvidenceNotes = domainEvidence.Notes ?? string.Empty;
        EvidenceConfidenceText = domainEvidence.Confidence?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private EvidenceLinkSummaryViewModel BuildEvidenceLinkSummary(Domain.EvidenceLinks.EvidenceLink link)
    {
        var evidenceTitle = Workspace.Evidence.FirstOrDefault(evidence => evidence.Id == link.EvidenceId)?.Title ?? link.EvidenceId.ToString();
        var targetDisplay = link.TargetKind switch
        {
            EvidenceLinkTargetKind.Entity => Workspace.Entities.FirstOrDefault(entity => entity.Id == link.TargetId)?.Name ?? link.TargetId.ToString(),
            EvidenceLinkTargetKind.Relationship => Workspace.Relationships
                .Where(relationship => relationship.Id == link.TargetId)
                .Select(relationship => $"{ResolveEntityName(relationship.SourceEntityId)} -> {relationship.RelationshipType} -> {ResolveEntityName(relationship.TargetEntityId)}")
                .FirstOrDefault() ?? link.TargetId.ToString(),
            EvidenceLinkTargetKind.Event => Workspace.Events.FirstOrDefault(@event => @event.Id == link.TargetId)?.Title ?? link.TargetId.ToString(),
            _ => link.TargetId.ToString()
        };

        return new EvidenceLinkSummaryViewModel(link.Id, evidenceTitle, link.TargetKind, targetDisplay, link.Role, link.Notes, link.Confidence);
    }

    private IReadOnlyList<LinkedEvidenceSummaryViewModel> BuildLinkedEvidenceViewModels(IReadOnlyList<LinkedEvidenceSummary> linkedEvidence) =>
        linkedEvidence
            .Select(item => new LinkedEvidenceSummaryViewModel(item.EvidenceLinkId, item.Title, item.Citation, item.Role, item.LinkNotes, item.LinkConfidence))
            .ToArray();

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

    private void EventPanelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EventPanelViewModel.SelectedEvent))
        {
            OnPropertyChanged(nameof(EventPanel));
        }
    }

    private static void ReplaceCollection<T>(ObservableCollection<T> collection, IReadOnlyList<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }

    private void ClearEntityEditor()
    {
        EditableEntityName = string.Empty;
        EditableEntityType = string.Empty;
        EditableEntityNotes = string.Empty;
        EditableEntityConfidenceText = string.Empty;
    }

    private void ClearEvidenceEditor()
    {
        EvidenceTitle = string.Empty;
        EvidenceCitation = string.Empty;
        EvidenceNotes = string.Empty;
        EvidenceConfidenceText = string.Empty;
    }

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

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class EventPanelViewModel : INotifyPropertyChanged
{
    private EventSummaryViewModel? _selectedEvent;
    private string _eventTitle = string.Empty;
    private string _eventOccurredAtText = string.Empty;
    private string _eventNotes = string.Empty;
    private string _eventConfidenceText = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<EventSummaryViewModel> Events { get; } = [];

    public ObservableCollection<LinkedEvidenceSummaryViewModel> LinkedEvidence { get; } = [];

    public EventSummaryViewModel? SelectedEvent
    {
        get => _selectedEvent;
        set
        {
            if (SetField(ref _selectedEvent, value))
            {
                PopulateEditor(value);
                RefreshLinkedEvidence?.Invoke(value?.Id);
                OnPropertyChanged(nameof(EventActionLabel));
            }
        }
    }

    public string EventTitle
    {
        get => _eventTitle;
        set => SetField(ref _eventTitle, value);
    }

    public string EventOccurredAtText
    {
        get => _eventOccurredAtText;
        set => SetField(ref _eventOccurredAtText, value);
    }

    public string EventNotes
    {
        get => _eventNotes;
        set => SetField(ref _eventNotes, value);
    }

    public string EventConfidenceText
    {
        get => _eventConfidenceText;
        set => SetField(ref _eventConfidenceText, value);
    }

    public string EventActionLabel => SelectedEvent is null ? "Add Event" : "Update Event";

    public Func<Guid?, IReadOnlyList<LinkedEvidenceSummaryViewModel>>? RefreshLinkedEvidence { get; private set; }

    public void Refresh(
        IReadOnlyList<EventSummaryViewModel> events,
        Guid? selectedEventId,
        Func<Guid?, IReadOnlyList<LinkedEvidenceSummaryViewModel>> refreshLinkedEvidence)
    {
        RefreshLinkedEvidence = refreshLinkedEvidence;
        ReplaceCollection(Events, events);
        SelectedEvent = selectedEventId is null ? null : Events.FirstOrDefault(@event => @event.Id == selectedEventId);

        if (SelectedEvent is null)
        {
            ClearEditor();
            ReplaceCollection(LinkedEvidence, []);
        }
    }

    private void PopulateEditor(EventSummaryViewModel? summary)
    {
        if (summary is null)
        {
            ClearEditor();
            return;
        }

        EventTitle = summary.Title;
        EventOccurredAtText = summary.OccurredAtUtc?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty;
        EventNotes = summary.Notes ?? string.Empty;
        EventConfidenceText = summary.Confidence?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        ReplaceCollection(LinkedEvidence, RefreshLinkedEvidence?.Invoke(summary.Id) ?? []);
    }

    private void ClearEditor()
    {
        EventTitle = string.Empty;
        EventOccurredAtText = string.Empty;
        EventNotes = string.Empty;
        EventConfidenceText = string.Empty;
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static void ReplaceCollection<T>(ObservableCollection<T> collection, IReadOnlyList<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}

public sealed class EvidenceLinkPanelViewModel : INotifyPropertyChanged
{
    private Func<EvidenceLinkTargetKind, IReadOnlyList<TargetOptionViewModel>> _targetResolver = static _ => [];
    private EvidenceOptionViewModel? _selectedEvidence;
    private EvidenceLinkTargetKindOptionViewModel? _selectedTargetKindOption;
    private TargetOptionViewModel? _selectedTarget;
    private EvidenceLinkSummaryViewModel? _selectedLink;
    private string _linkRole = string.Empty;
    private string _linkNotes = string.Empty;
    private string _linkConfidenceText = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<EvidenceOptionViewModel> EvidenceOptions { get; } = [];

    public ObservableCollection<EvidenceLinkTargetKindOptionViewModel> TargetKindOptions { get; } = [];

    public ObservableCollection<TargetOptionViewModel> TargetOptions { get; } = [];

    public ObservableCollection<EvidenceLinkSummaryViewModel> Links { get; } = [];

    public EvidenceOptionViewModel? SelectedEvidence
    {
        get => _selectedEvidence;
        set => SetField(ref _selectedEvidence, value);
    }

    public EvidenceLinkTargetKindOptionViewModel? SelectedTargetKindOption
    {
        get => _selectedTargetKindOption;
        set
        {
            if (SetField(ref _selectedTargetKindOption, value))
            {
                UpdateTargets(_targetResolver(value?.Kind ?? EvidenceLinkTargetKind.Entity));
            }
        }
    }

    public TargetOptionViewModel? SelectedTarget
    {
        get => _selectedTarget;
        set => SetField(ref _selectedTarget, value);
    }

    public EvidenceLinkSummaryViewModel? SelectedLink
    {
        get => _selectedLink;
        set => SetField(ref _selectedLink, value);
    }

    public string LinkRole
    {
        get => _linkRole;
        set => SetField(ref _linkRole, value);
    }

    public string LinkNotes
    {
        get => _linkNotes;
        set => SetField(ref _linkNotes, value);
    }

    public string LinkConfidenceText
    {
        get => _linkConfidenceText;
        set => SetField(ref _linkConfidenceText, value);
    }

    public void Refresh(
        IReadOnlyList<EvidenceOptionViewModel> evidenceOptions,
        IReadOnlyList<EvidenceLinkTargetKindOptionViewModel> targetKindOptions,
        IReadOnlyList<TargetOptionViewModel> defaultTargets,
        IReadOnlyList<EvidenceLinkSummaryViewModel> links,
        Guid? selectedLinkId)
    {
        ReplaceCollection(EvidenceOptions, evidenceOptions);
        ReplaceCollection(TargetKindOptions, targetKindOptions);
        ReplaceCollection(TargetOptions, defaultTargets);
        ReplaceCollection(Links, links);

        SelectedEvidence ??= EvidenceOptions.FirstOrDefault();
        SelectedTargetKindOption ??= TargetKindOptions.FirstOrDefault();
        SelectedTarget ??= TargetOptions.FirstOrDefault();
        SelectedLink = selectedLinkId is null ? null : Links.FirstOrDefault(link => link.Id == selectedLinkId);
    }

    public void SetTargetResolver(Func<EvidenceLinkTargetKind, IReadOnlyList<TargetOptionViewModel>> resolver)
    {
        _targetResolver = resolver;
        if (SelectedTargetKindOption is not null)
        {
            UpdateTargets(_targetResolver(SelectedTargetKindOption.Kind));
        }
    }

    public void UpdateTargets(IReadOnlyList<TargetOptionViewModel> targets)
    {
        ReplaceCollection(TargetOptions, targets);
        SelectedTarget = TargetOptions.FirstOrDefault();
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static void ReplaceCollection<T>(ObservableCollection<T> collection, IReadOnlyList<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}

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
}

public sealed record LinkedEvidenceSummaryViewModel(Guid EvidenceLinkId, string Title, string? Citation, string? Role, string? Notes, double? Confidence)
{
    public string DisplayName => string.IsNullOrWhiteSpace(Role) ? Title : $"{Title} ({Role})";
}

public sealed record EvidenceOptionViewModel(Guid Id, string DisplayName);

public sealed record TargetOptionViewModel(Guid Id, string DisplayName);

public sealed record EvidenceLinkTargetKindOptionViewModel(EvidenceLinkTargetKind Kind, string DisplayName);

public sealed record EvidenceLinkSummaryViewModel(Guid Id, string EvidenceTitle, EvidenceLinkTargetKind TargetKind, string TargetDisplayName, string? Role, string? Notes, double? Confidence)
{
    public string DisplayName => $"{EvidenceTitle} -> {TargetKind} -> {TargetDisplayName}";
}
