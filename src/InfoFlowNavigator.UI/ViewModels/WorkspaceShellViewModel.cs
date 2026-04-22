using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using InfoFlowNavigator.Application.Abstractions;
using InfoFlowNavigator.Application.Analysis;
using InfoFlowNavigator.Application.Workspaces;
using InfoFlowNavigator.Domain.Claims;
using InfoFlowNavigator.Domain.EvidenceLinks;
using InfoFlowNavigator.Domain.Hypotheses;
using InfoFlowNavigator.Domain.Workspaces;

namespace InfoFlowNavigator.UI.ViewModels;

public sealed class WorkspaceShellViewModel : ViewModelBase
{
    private readonly WorkspaceApplicationService _workspaceService;
    private readonly IAnalysisService _analysisService;
    private readonly IReportGenerator _reportGenerator;
    private readonly IWorkspaceExportService _workspaceExportService;
    private readonly IWorkspaceFileDialogService _workspaceFileDialogService;
    private AnalysisWorkspace _workspace;
    private WorkspaceAnalysisResult _analysis;
    private string _workspaceName = string.Empty;
    private string _workspacePath = string.Empty;
    private string _statusMessage;
    private WorkbenchSectionItemViewModel? _selectedSection;
    private string? _lastNetworkExportPath;
    private bool _isRightDrawerOpen = true;
    private bool _isSpotlightComposerOpen;
    private SpotlightComposerMode _spotlightMode;
    private bool _isQuickCaptureExpanded;
    private Guid? _recentlyChangedItemId;
    private string? _recentlyChangedItemKind;

    public WorkspaceShellViewModel(
        WorkspaceApplicationService workspaceService,
        IAnalysisService analysisService,
        IReportGenerator reportGenerator,
        IWorkspaceExportService workspaceExportService,
        IWorkspaceFileDialogService workspaceFileDialogService)
    {
        _workspaceService = workspaceService;
        _analysisService = analysisService;
        _reportGenerator = reportGenerator;
        _workspaceExportService = workspaceExportService;
        _workspaceFileDialogService = workspaceFileDialogService;
        _workspace = workspaceService.CreateWorkspace("Untitled Workspace");
        _analysis = _analysisService.SummarizeAsync(_workspace).GetAwaiter().GetResult();
        _statusMessage = "Create or open a workspace to begin.";

        Sections = new ObservableCollection<WorkbenchSectionItemViewModel>
        {
            new(WorkbenchSection.Overview, "Overview", "Case summary and guidance", "OV"),
            new(WorkbenchSection.Entities, "Entities", "People, organizations, and assets", "EN"),
            new(WorkbenchSection.Relationships, "Relationships", "How subjects are connected", "RE"),
            new(WorkbenchSection.Events, "Events", "Observed activity and chronology", "EV"),
            new(WorkbenchSection.Claims, "Claims", "First-class assertions and support", "CL"),
            new(WorkbenchSection.Hypotheses, "Hypotheses", "Competing explanations and inference", "HY"),
            new(WorkbenchSection.Evidence, "Evidence", "Sources and structured assessments", "ED"),
            new(WorkbenchSection.Findings, "Findings", "Explainable analysis guidance", "FI")
        };

        Toasts = [];
        InsightPulseItems = [];

        NewWorkspaceCommand = new RelayCommand(() => ExecuteSafely(CreateNewWorkspace));
        OpenWorkspaceCommand = new AsyncRelayCommand(() => ExecuteSafelyAsync(OpenWorkspaceAsync));
        SaveWorkspaceCommand = new AsyncRelayCommand(() => ExecuteSafelyAsync(SaveWorkspaceAsync));
        ExportBriefingCommand = new AsyncRelayCommand(() => ExecuteSafelyAsync(ExportBriefingAsync));
        ExportNetworkCommand = new AsyncRelayCommand(() => ExecuteSafelyAsync(ExportNetworkAsync));

        ShowOverviewCommand = new RelayCommand(() => SelectSection(WorkbenchSection.Overview));
        ShowEntitiesCommand = new RelayCommand(() => SelectSection(WorkbenchSection.Entities));
        ShowRelationshipsCommand = new RelayCommand(() => SelectSection(WorkbenchSection.Relationships));
        ShowEventsCommand = new RelayCommand(() => SelectSection(WorkbenchSection.Events));
        ShowClaimsCommand = new RelayCommand(() => SelectSection(WorkbenchSection.Claims));
        ShowHypothesesCommand = new RelayCommand(() => SelectSection(WorkbenchSection.Hypotheses));
        ShowEvidenceCommand = new RelayCommand(() => SelectSection(WorkbenchSection.Evidence));
        ShowFindingsCommand = new RelayCommand(() => SelectSection(WorkbenchSection.Findings));

        ToggleRightDrawerCommand = new RelayCommand(() => IsRightDrawerOpen = !IsRightDrawerOpen);
        OpenQuickCaptureCommand = new RelayCommand(() => IsQuickCaptureExpanded = true);
        CloseQuickCaptureCommand = new RelayCommand(() => IsQuickCaptureExpanded = false);
        BeginQuickAddEntityCommand = new RelayCommand(() => ExecuteSafely(BeginQuickAddEntity));
        BeginQuickAddEventCommand = new RelayCommand(() => ExecuteSafely(BeginNewEvent));
        BeginQuickAddClaimCommand = new RelayCommand(() => ExecuteSafely(BeginNewClaim));
        BeginQuickAddHypothesisCommand = new RelayCommand(() => ExecuteSafely(BeginNewHypothesis));
        BeginQuickAddEvidenceCommand = new RelayCommand(() => ExecuteSafely(BeginNewEvidence));
        CloseSpotlightComposerCommand = new RelayCommand(CloseSpotlightComposer);

        Overview = new OverviewViewModel();
        Entities = new EntitiesViewModel(
            new RelayCommand(() => ExecuteSafely(AddEntity)),
            new RelayCommand(() => ExecuteSafely(UpdateSelectedEntity)),
            new RelayCommand(() => ExecuteSafely(DeleteSelectedEntity)),
            _ => RefreshEntityLinkedEvidence());
        Relationships = new RelationshipsViewModel(
            new RelayCommand(() => ExecuteSafely(SaveRelationship)),
            new RelayCommand(() => ExecuteSafely(DeleteSelectedRelationship)),
            _ => RefreshRelationshipLinkedEvidence());
        Events = new EventsViewModel(
            new RelayCommand(() => ExecuteSafely(BeginNewEvent)),
            new RelayCommand(() => ExecuteSafely(SaveEvent)),
            new RelayCommand(() => ExecuteSafely(DeleteSelectedEvent)),
            new RelayCommand(() => ExecuteSafely(AddEventParticipant)),
            new RelayCommand(() => ExecuteSafely(DeleteSelectedEventParticipant)),
            _ =>
            {
                RefreshEventLinkedEvidence();
                RefreshEventParticipants();
            });
        Claims = new ClaimsViewModel(
            new RelayCommand(() => ExecuteSafely(BeginNewClaim)),
            new RelayCommand(() => ExecuteSafely(SaveClaim)),
            new RelayCommand(() => ExecuteSafely(DeleteSelectedClaim)),
            _ => RefreshClaimLinkedEvidence());
        Hypotheses = new HypothesesViewModel(
            new RelayCommand(() => ExecuteSafely(BeginNewHypothesis)),
            new RelayCommand(() => ExecuteSafely(SaveHypothesis)),
            new RelayCommand(() => ExecuteSafely(DeleteSelectedHypothesis)),
            _ => RefreshHypothesisEvidence());
        Evidence = new EvidenceViewModel(
            new RelayCommand(() => ExecuteSafely(BeginNewEvidence)),
            new RelayCommand(() => ExecuteSafely(SaveEvidence)),
            new RelayCommand(() => ExecuteSafely(DeleteSelectedEvidence)),
            new RelayCommand(() => ExecuteSafely(BeginNewEvidenceAssessment)),
            new RelayCommand(() => ExecuteSafely(SaveEvidenceAssessment)),
            new RelayCommand(() => ExecuteSafely(DeleteSelectedEvidenceAssessment)),
            _ => RefreshEvidenceAssessmentsForSelection(),
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
        set
        {
            if (SetProperty(ref _workspacePath, value))
            {
                OnPropertyChanged(nameof(WorkspacePathDisplay));
            }
        }
    }

    public string WorkspacePathDisplay => string.IsNullOrWhiteSpace(WorkspacePath) ? "No workspace file selected." : WorkspacePath;

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
                OnPropertyChanged(nameof(WorkspaceParticipantCount));
                OnPropertyChanged(nameof(WorkspaceClaimCount));
                OnPropertyChanged(nameof(WorkspaceHypothesisCount));
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
    public int WorkspaceParticipantCount => Workspace.EventParticipants.Count;
    public int WorkspaceClaimCount => Workspace.Claims.Count;
    public int WorkspaceHypothesisCount => Workspace.Hypotheses.Count;
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
                OnPropertyChanged(nameof(IsClaimsMode));
                OnPropertyChanged(nameof(IsHypothesesMode));
                OnPropertyChanged(nameof(IsEvidenceMode));
                OnPropertyChanged(nameof(IsFindingsMode));
                OnPropertyChanged(nameof(IsInspectorVisible));
                OnPropertyChanged(nameof(CurrentDrawerTitle));
                OnPropertyChanged(nameof(CurrentDrawerHint));
            }
        }
    }

    public string CurrentSectionTitle => SelectedSection?.Title ?? "Overview";
    public string CurrentSectionDescription => SelectedSection?.Description ?? "Case summary and guidance";
    public bool IsOverviewMode => SelectedSection?.Section == WorkbenchSection.Overview;
    public bool IsEntitiesMode => SelectedSection?.Section == WorkbenchSection.Entities;
    public bool IsRelationshipsMode => SelectedSection?.Section == WorkbenchSection.Relationships;
    public bool IsEventsMode => SelectedSection?.Section == WorkbenchSection.Events;
    public bool IsClaimsMode => SelectedSection?.Section == WorkbenchSection.Claims;
    public bool IsHypothesesMode => SelectedSection?.Section == WorkbenchSection.Hypotheses;
    public bool IsEvidenceMode => SelectedSection?.Section == WorkbenchSection.Evidence;
    public bool IsFindingsMode => SelectedSection?.Section == WorkbenchSection.Findings;

    public bool IsRightDrawerOpen
    {
        get => _isRightDrawerOpen;
        set
        {
            if (SetProperty(ref _isRightDrawerOpen, value))
            {
                OnPropertyChanged(nameof(IsInspectorVisible));
                OnPropertyChanged(nameof(RightDrawerButtonLabel));
            }
        }
    }

    public bool IsSpotlightComposerOpen
    {
        get => _isSpotlightComposerOpen;
        set => SetProperty(ref _isSpotlightComposerOpen, value);
    }

    public SpotlightComposerMode SpotlightMode
    {
        get => _spotlightMode;
        set
        {
            if (SetProperty(ref _spotlightMode, value))
            {
                OnPropertyChanged(nameof(SpotlightTitle));
                OnPropertyChanged(nameof(SpotlightDescription));
                OnPropertyChanged(nameof(IsEntitySpotlight));
                OnPropertyChanged(nameof(IsEventSpotlight));
                OnPropertyChanged(nameof(IsClaimSpotlight));
                OnPropertyChanged(nameof(IsHypothesisSpotlight));
                OnPropertyChanged(nameof(IsEvidenceSpotlight));
                OnPropertyChanged(nameof(IsRelationshipSpotlight));
                OnPropertyChanged(nameof(IsEventParticipantSpotlight));
            }
        }
    }

    public bool IsQuickCaptureExpanded
    {
        get => _isQuickCaptureExpanded;
        set => SetProperty(ref _isQuickCaptureExpanded, value);
    }

    public ObservableCollection<ShellToastViewModel> Toasts { get; }

    public ObservableCollection<InsightPulseItemViewModel> InsightPulseItems { get; }

    public Guid? RecentlyChangedItemId
    {
        get => _recentlyChangedItemId;
        private set => SetProperty(ref _recentlyChangedItemId, value);
    }

    public string? RecentlyChangedItemKind
    {
        get => _recentlyChangedItemKind;
        private set => SetProperty(ref _recentlyChangedItemKind, value);
    }

    public bool HasInsightPulseItems => InsightPulseItems.Count > 0;
    public bool HasToasts => Toasts.Count > 0;
    public bool IsInspectorVisible => IsRightDrawerOpen && !IsOverviewMode;
    public string RightDrawerButtonLabel => IsRightDrawerOpen ? "Hide Drawer" : "Show Drawer";

    public string CurrentDrawerTitle =>
        SelectedSection?.Section switch
        {
            WorkbenchSection.Entities => Entities.InspectorTitle,
            WorkbenchSection.Relationships => Relationships.InspectorTitle,
            WorkbenchSection.Events => Events.EditorTitle,
            WorkbenchSection.Claims => Claims.EditorTitle,
            WorkbenchSection.Hypotheses => Hypotheses.EditorTitle,
            WorkbenchSection.Evidence => Evidence.EditorTitle,
            WorkbenchSection.Findings => "Analysis Drawer",
            _ => "Inspector"
        };

    public string CurrentDrawerHint =>
        SelectedSection?.Section switch
        {
            WorkbenchSection.Entities => Entities.InspectorHint,
            WorkbenchSection.Relationships => Relationships.InspectorHint,
            WorkbenchSection.Events => Events.EditorHint,
            WorkbenchSection.Claims => Claims.EditorHint,
            WorkbenchSection.Hypotheses => Hypotheses.EditorHint,
            WorkbenchSection.Evidence => Evidence.EditorHint,
            WorkbenchSection.Findings => Findings.TopPrioritySummary,
            _ => "Context for the active section."
        };

    public string SpotlightTitle =>
        SpotlightMode switch
        {
            SpotlightComposerMode.Entity => "Quick Capture Entity",
            SpotlightComposerMode.Event => "Compose Event",
            SpotlightComposerMode.Claim => "Compose Claim",
            SpotlightComposerMode.Hypothesis => "Compose Hypothesis",
            SpotlightComposerMode.Evidence => "Compose Evidence",
            SpotlightComposerMode.Relationship => "Compose Relationship",
            SpotlightComposerMode.EventParticipant => "Add Event Participant",
            _ => "Spotlight Composer"
        };

    public string SpotlightDescription =>
        SpotlightMode switch
        {
            SpotlightComposerMode.Entity => "Capture a new person, organization, or asset from anywhere in the workspace.",
            SpotlightComposerMode.Event => "Build an event in a focused composer without leaving the current flow.",
            SpotlightComposerMode.Claim => "Capture a crisp analytic assertion and route it into the right section.",
            SpotlightComposerMode.Hypothesis => "Draft or refine a hypothesis in a centered high-focus editor.",
            SpotlightComposerMode.Evidence => "Bring source material in quickly, then attach assessments after save.",
            SpotlightComposerMode.Relationship => "Capture a relationship while keeping the rest of the shell quiet.",
            SpotlightComposerMode.EventParticipant => "Add participant context without losing chronology focus.",
            _ => "Focused composition surface."
        };

    public bool IsEntitySpotlight => SpotlightMode == SpotlightComposerMode.Entity;
    public bool IsEventSpotlight => SpotlightMode == SpotlightComposerMode.Event;
    public bool IsClaimSpotlight => SpotlightMode == SpotlightComposerMode.Claim;
    public bool IsHypothesisSpotlight => SpotlightMode == SpotlightComposerMode.Hypothesis;
    public bool IsEvidenceSpotlight => SpotlightMode == SpotlightComposerMode.Evidence;
    public bool IsRelationshipSpotlight => SpotlightMode == SpotlightComposerMode.Relationship;
    public bool IsEventParticipantSpotlight => SpotlightMode == SpotlightComposerMode.EventParticipant;

    public OverviewViewModel Overview { get; }
    public EntitiesViewModel Entities { get; }
    public RelationshipsViewModel Relationships { get; }
    public EventsViewModel Events { get; }
    public ClaimsViewModel Claims { get; }
    public HypothesesViewModel Hypotheses { get; }
    public EvidenceViewModel Evidence { get; }
    public FindingsViewModel Findings { get; }

    public RelayCommand NewWorkspaceCommand { get; }
    public AsyncRelayCommand OpenWorkspaceCommand { get; }
    public AsyncRelayCommand SaveWorkspaceCommand { get; }
    public AsyncRelayCommand ExportBriefingCommand { get; }
    public AsyncRelayCommand ExportNetworkCommand { get; }
    public RelayCommand ShowOverviewCommand { get; }
    public RelayCommand ShowEntitiesCommand { get; }
    public RelayCommand ShowRelationshipsCommand { get; }
    public RelayCommand ShowEventsCommand { get; }
    public RelayCommand ShowClaimsCommand { get; }
    public RelayCommand ShowHypothesesCommand { get; }
    public RelayCommand ShowEvidenceCommand { get; }
    public RelayCommand ShowFindingsCommand { get; }
    public RelayCommand ToggleRightDrawerCommand { get; }
    public RelayCommand OpenQuickCaptureCommand { get; }
    public RelayCommand CloseQuickCaptureCommand { get; }
    public RelayCommand BeginQuickAddEntityCommand { get; }
    public RelayCommand BeginQuickAddEventCommand { get; }
    public RelayCommand BeginQuickAddClaimCommand { get; }
    public RelayCommand BeginQuickAddHypothesisCommand { get; }
    public RelayCommand BeginQuickAddEvidenceCommand { get; }
    public RelayCommand CloseSpotlightComposerCommand { get; }

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
        _lastNetworkExportPath = null;
        WorkspacePath = string.Empty;
        SetWorkspace(_workspaceService.CreateWorkspace(name));
        CloseSpotlightComposer();
        StatusMessage = "Created a new workspace.";
    }

    private async Task OpenWorkspaceAsync()
    {
        var selectedPath = await _workspaceFileDialogService.PickOpenWorkspacePathAsync();
        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            StatusMessage = "Open workspace canceled.";
            return;
        }

        _lastNetworkExportPath = null;
        WorkspacePath = selectedPath;
        SetWorkspace(await _workspaceService.OpenAsync(WorkspacePath.Trim()));
        StatusMessage = $"Opened workspace from '{WorkspacePath}'.";
    }

    private async Task SaveWorkspaceAsync()
    {
        var savePath = await EnsureWorkspacePathForSaveAsync();
        if (string.IsNullOrWhiteSpace(savePath))
        {
            StatusMessage = "Save workspace canceled.";
            return;
        }

        SetWorkspace(_workspaceService.RenameWorkspace(Workspace, WorkspaceName));
        WorkspacePath = savePath;
        await _workspaceService.SaveAsync(WorkspacePath.Trim(), Workspace);
        StatusMessage = $"Saved workspace to '{WorkspacePath}'.";
        EnqueueToast("Workspace Saved", "All changes were written to the selected workspace file.", "Success", "Workspace");
    }

    private async Task ExportBriefingAsync()
    {
        var savePath = await EnsureWorkspacePathForSaveAsync();
        if (string.IsNullOrWhiteSpace(savePath))
        {
            StatusMessage = "Briefing export canceled.";
            return;
        }

        WorkspacePath = savePath;
        var artifact = await _reportGenerator.GenerateAsync(Workspace);
        var outputPath = BuildSiblingArtifactPath(".briefing.txt");
        var content = artifact.Content;
        if (!string.IsNullOrWhiteSpace(_lastNetworkExportPath))
        {
            content += $"{Environment.NewLine}Network Export Note{Environment.NewLine}- Last network export: {_lastNetworkExportPath}{Environment.NewLine}";
        }

        await File.WriteAllTextAsync(outputPath, content);
        StatusMessage = $"Exported analyst briefing to '{outputPath}'.";
        EnqueueToast("Briefing Exported", $"Created analyst briefing at '{outputPath}'.", "Info", "Briefing");
    }

    private async Task ExportNetworkAsync()
    {
        var savePath = await EnsureWorkspacePathForSaveAsync();
        if (string.IsNullOrWhiteSpace(savePath))
        {
            StatusMessage = "Network export canceled.";
            return;
        }

        WorkspacePath = savePath;
        var outputPath = BuildSiblingArtifactPath(".network.json");
        await _workspaceExportService.ExportAsync(Workspace, outputPath);
        _lastNetworkExportPath = outputPath;
        StatusMessage = $"Exported MedWNetwork-compatible network JSON to '{outputPath}'.";
        EnqueueToast("Network Exported", $"Created network artifact at '{outputPath}'.", "Info", "Network");
    }

    private void AddEntity()
    {
        var updatedWorkspace = _workspaceService.AddEntity(Workspace, Entities.NewEntityName, Entities.NewEntityType);
        var createdEntityId = FindAddedId(Workspace.Entities.Select(item => item.Id), updatedWorkspace.Entities.Select(item => item.Id));
        SetWorkspace(updatedWorkspace);
        Entities.ClearAddForm();
        Entities.SelectedEntity = Entities.Entities.FirstOrDefault(item => item.Id == createdEntityId);
        RegisterRecentChange(createdEntityId, "Entity");
        EnqueueToast("Entity Captured", "Added a new entity to the workspace.", "Success", "Entity");
        CloseSpotlightComposer();
        StatusMessage = "Added entity to the workspace.";
    }

    private void UpdateSelectedEntity()
    {
        if (Entities.SelectedEntity is null)
        {
            throw new InvalidOperationException("Select an entity to update.");
        }

        var confidence = ParseOptionalConfidence(Entities.EditorConfidenceText, "Entity confidence");
        var entityId = Entities.SelectedEntity.Id;
        SetWorkspace(_workspaceService.UpdateEntity(Workspace, entityId, Entities.EditorName, Entities.EditorType, Entities.EditorNotes, confidence));
        RegisterRecentChange(entityId, "Entity");
        EnqueueToast("Entity Updated", "Saved the selected entity.", "Success", "Entity");
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

    private void SaveRelationship()
    {
        if (Relationships.SelectedSource is null || Relationships.SelectedTarget is null)
        {
            throw new InvalidOperationException("Select both source and target entities.");
        }

        var confidence = ParseOptionalConfidence(Relationships.RelationshipConfidenceText, "Relationship confidence");

        if (Relationships.SelectedRelationship is null)
        {
            var updatedWorkspace = _workspaceService.AddRelationship(
                Workspace,
                Relationships.SelectedSource.Id,
                Relationships.SelectedTarget.Id,
                Relationships.RelationshipType,
                Relationships.RelationshipNotes,
                confidence);
            var createdRelationshipId = FindAddedId(Workspace.Relationships.Select(item => item.Id), updatedWorkspace.Relationships.Select(item => item.Id));
            SetWorkspace(updatedWorkspace);
            Relationships.SelectedRelationship = Relationships.Relationships.FirstOrDefault(item => item.Id == createdRelationshipId);
            RegisterRecentChange(createdRelationshipId, "Relationship");
            EnqueueToast("Relationship Added", "Captured a new relationship in the workspace.", "Success", "Relationship");
            CloseSpotlightComposer();
            StatusMessage = "Added relationship to the workspace.";
            return;
        }

        var relationshipId = Relationships.SelectedRelationship.Id;
        SetWorkspace(_workspaceService.UpdateRelationship(
            Workspace,
            relationshipId,
            Relationships.SelectedSource.Id,
            Relationships.SelectedTarget.Id,
            Relationships.RelationshipType,
            Relationships.RelationshipNotes,
            confidence));
        RegisterRecentChange(relationshipId, "Relationship");
        EnqueueToast("Relationship Updated", "Saved the selected relationship.", "Success", "Relationship");
        StatusMessage = "Updated selected relationship.";
    }

    private void DeleteSelectedRelationship()
    {
        if (Relationships.SelectedRelationship is null)
        {
            throw new InvalidOperationException("Select a relationship to delete.");
        }

        SetWorkspace(_workspaceService.RemoveRelationship(Workspace, Relationships.SelectedRelationship.Id));
        Relationships.BeginNewRelationship();
        StatusMessage = "Deleted selected relationship.";
    }

    private void SaveEvent()
    {
        var confidence = ParseOptionalConfidence(Events.EventConfidenceText, "Event confidence");
        var occurredAtUtc = EventOccurredAtFormatting.ParseRequired(Events.EventOccurredAtText);

        if (!Events.IsEditingExistingItem)
        {
            var updatedWorkspace = _workspaceService.AddEvent(Workspace, Events.EventTitle, occurredAtUtc, Events.EventNotes, confidence);
            var createdEventId = FindAddedId(Workspace.Events.Select(item => item.Id), updatedWorkspace.Events.Select(item => item.Id));
            SetWorkspace(updatedWorkspace);
            SelectEvent(createdEventId);
            RegisterRecentChange(createdEventId, "Event");
            EnqueueToast("Event Added", "Captured a new event.", "Success", "Event");
            CloseSpotlightComposer();
            StatusMessage = "Added event.";
            return;
        }

        if (Events.SelectedEvent is null)
        {
            throw new InvalidOperationException("Select an event to update or click New Event to add one.");
        }

        var eventId = Events.SelectedEvent.Id;
        SetWorkspace(_workspaceService.UpdateEvent(Workspace, eventId, Events.EventTitle, occurredAtUtc, Events.EventNotes, confidence));
        RegisterRecentChange(eventId, "Event");
        EnqueueToast("Event Updated", "Saved the selected event.", "Success", "Event");
        StatusMessage = "Updated selected event.";
    }

    private void BeginQuickAddEntity()
    {
        SelectSection(WorkbenchSection.Entities);
        IsQuickCaptureExpanded = false;
        OpenSpotlight(SpotlightComposerMode.Entity);
        StatusMessage = "Ready to capture a new entity.";
    }

    private void BeginNewEvent()
    {
        SelectSection(WorkbenchSection.Events);
        Events.BeginNewEvent();
        IsQuickCaptureExpanded = false;
        OpenSpotlight(SpotlightComposerMode.Event);
        StatusMessage = "Ready to add a new event.";
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

    private void AddEventParticipant()
    {
        if (Events.SelectedEvent is null)
        {
            StatusMessage = "Select an event first.";
            return;
        }

        if (Events.SelectedParticipantEntity is null)
        {
            StatusMessage = "Select a participant.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Events.ParticipantRole))
        {
            StatusMessage = "Participant role is required.";
            return;
        }

        var confidence = ParseOptionalConfidence(Events.ParticipantConfidenceText, "Participant confidence");
        if (Events.SelectedParticipant is null)
        {
            var updatedWorkspace = _workspaceService.AddEventParticipant(
                Workspace,
                Events.SelectedEvent.Id,
                Events.SelectedParticipantEntity.Id,
                Events.ParticipantRole,
                confidence,
                Events.ParticipantNotes);
            var createdParticipantId = FindAddedId(Workspace.EventParticipants.Select(item => item.Id), updatedWorkspace.EventParticipants.Select(item => item.Id));
            SetWorkspace(updatedWorkspace);
            Events.ClearParticipantEditor();
            RegisterRecentChange(createdParticipantId, "Event Participant");
            EnqueueToast("Participant Added", "Linked a participant to the selected event.", "Success", "Event Participant");
            CloseSpotlightComposer();
            StatusMessage = "Added event participant.";
            return;
        }

        var participantId = Events.SelectedParticipant.Id;
        SetWorkspace(_workspaceService.UpdateEventParticipant(
            Workspace,
            participantId,
            Events.ParticipantRole,
            confidence,
            Events.ParticipantNotes));
        RegisterRecentChange(participantId, "Event Participant");
        EnqueueToast("Participant Updated", "Saved the selected event participant.", "Success", "Event Participant");
        StatusMessage = "Updated event participant.";
    }

    private void DeleteSelectedEventParticipant()
    {
        if (Events.SelectedParticipant is null)
        {
            throw new InvalidOperationException("Select an event participant to remove.");
        }

        SetWorkspace(_workspaceService.RemoveEventParticipant(Workspace, Events.SelectedParticipant.Id));
        Events.ClearParticipantEditor();
        StatusMessage = "Removed event participant.";
    }

    private void BeginNewClaim()
    {
        SelectSection(WorkbenchSection.Claims);
        Claims.BeginNewClaim();
        IsQuickCaptureExpanded = false;
        OpenSpotlight(SpotlightComposerMode.Claim);
        StatusMessage = "Ready to add a new claim.";
    }

    private void SaveClaim()
    {
        var confidence = ParseOptionalConfidence(Claims.ConfidenceText, "Claim confidence");
        var claimType = Claims.SelectedClaimType?.ClaimType ?? ClaimType.General;
        var claimStatus = Claims.SelectedClaimStatus?.Status ?? ClaimStatus.Draft;
        var targetKind = Claims.SelectedTargetKind?.Kind;
        var targetId = Claims.SelectedTarget?.Id;
        var hypothesisId = Claims.SelectedHypothesis?.Id;

        if (!Claims.IsEditingExistingItem)
        {
            var updatedWorkspace = _workspaceService.AddClaim(
                Workspace,
                Claims.Statement,
                claimType,
                claimStatus,
                confidence,
                Claims.Notes,
                targetKind,
                targetId,
                hypothesisId);
            var createdClaimId = FindAddedId(Workspace.Claims.Select(item => item.Id), updatedWorkspace.Claims.Select(item => item.Id));
            SetWorkspace(updatedWorkspace);
            SelectClaim(createdClaimId);
            RegisterRecentChange(createdClaimId, "Claim");
            EnqueueToast("Claim Added", "Captured a new claim.", "Success", "Claim");
            CloseSpotlightComposer();
            StatusMessage = "Added claim.";
            return;
        }

        if (Claims.SelectedClaim is null)
        {
            throw new InvalidOperationException("Select a claim to update or click New Claim to add one.");
        }

        var claimId = Claims.SelectedClaim.Id;
        SetWorkspace(_workspaceService.UpdateClaim(
            Workspace,
            claimId,
            Claims.Statement,
            claimType,
            claimStatus,
            confidence,
            Claims.Notes,
            targetKind,
            targetId,
            hypothesisId));
        RegisterRecentChange(claimId, "Claim");
        EnqueueToast("Claim Updated", "Saved the selected claim.", "Success", "Claim");
        StatusMessage = "Updated selected claim.";
    }

    private void BeginNewHypothesis()
    {
        SelectSection(WorkbenchSection.Hypotheses);
        Hypotheses.BeginNewHypothesis();
        IsQuickCaptureExpanded = false;
        OpenSpotlight(SpotlightComposerMode.Hypothesis);
        StatusMessage = "Ready to add a new hypothesis.";
    }

    private void DeleteSelectedClaim()
    {
        if (Claims.SelectedClaim is null)
        {
            throw new InvalidOperationException("Select a claim to delete.");
        }

        var statement = Claims.SelectedClaim.Statement;
        SetWorkspace(_workspaceService.RemoveClaim(Workspace, Claims.SelectedClaim.Id));
        StatusMessage = $"Deleted claim '{statement}'.";
    }

    private void SaveHypothesis()
    {
        if (string.IsNullOrWhiteSpace(Hypotheses.Title) || string.IsNullOrWhiteSpace(Hypotheses.Statement))
        {
            StatusMessage = "Hypothesis title and statement are required.";
            return;
        }

        var confidence = ParseOptionalConfidence(Hypotheses.ConfidenceText, "Hypothesis confidence");
        var status = Hypotheses.SelectedStatus?.Status ?? HypothesisStatus.Draft;

        if (!Hypotheses.IsEditingExistingItem)
        {
            var updatedWorkspace = _workspaceService.AddHypothesis(Workspace, Hypotheses.Title, Hypotheses.Statement, status, confidence, Hypotheses.Notes);
            var createdHypothesisId = FindAddedId(Workspace.Hypotheses.Select(item => item.Id), updatedWorkspace.Hypotheses.Select(item => item.Id));
            SetWorkspace(updatedWorkspace);
            SelectHypothesis(createdHypothesisId);
            RegisterRecentChange(createdHypothesisId, "Hypothesis");
            EnqueueToast("Hypothesis Added", "Captured a new hypothesis.", "Success", "Hypothesis");
            CloseSpotlightComposer();
            StatusMessage = "Added hypothesis.";
            return;
        }

        if (Hypotheses.SelectedHypothesis is null)
        {
            throw new InvalidOperationException("Select a hypothesis to update or click New Hypothesis to add one.");
        }

        var hypothesisId = Hypotheses.SelectedHypothesis.Id;
        SetWorkspace(_workspaceService.UpdateHypothesis(Workspace, hypothesisId, Hypotheses.Title, Hypotheses.Statement, status, confidence, Hypotheses.Notes));
        RegisterRecentChange(hypothesisId, "Hypothesis");
        EnqueueToast("Hypothesis Updated", "Saved the selected hypothesis.", "Success", "Hypothesis");
        StatusMessage = "Updated selected hypothesis.";
    }

    private void DeleteSelectedHypothesis()
    {
        if (Hypotheses.SelectedHypothesis is null)
        {
            throw new InvalidOperationException("Select a hypothesis to delete.");
        }

        var title = Hypotheses.SelectedHypothesis.Title;
        SetWorkspace(_workspaceService.RemoveHypothesis(Workspace, Hypotheses.SelectedHypothesis.Id));
        StatusMessage = $"Deleted hypothesis '{title}'.";
    }

    private void BeginNewEvidence()
    {
        SelectSection(WorkbenchSection.Evidence);
        Evidence.BeginNewEvidence();
        IsQuickCaptureExpanded = false;
        OpenSpotlight(SpotlightComposerMode.Evidence);
        StatusMessage = "Ready to add new evidence.";
    }

    private void BeginNewEvidenceAssessment()
    {
        if (Evidence.SelectedEvidence is null)
        {
            StatusMessage = "Select an evidence item first.";
            return;
        }

        SelectSection(WorkbenchSection.Evidence);
        Evidence.BeginNewAssessment();
        OpenSpotlight(SpotlightComposerMode.Evidence);
        StatusMessage = "Ready to add a new evidence assessment.";
    }

    private void SaveEvidence()
    {
        var confidence = ParseOptionalConfidence(Evidence.EvidenceConfidenceText, "Evidence confidence");

        if (!Evidence.IsEditingExistingItem)
        {
            var updatedWorkspace = _workspaceService.AddEvidence(Workspace, Evidence.EvidenceTitle, Evidence.EvidenceCitation, Evidence.EvidenceNotes, confidence);
            var createdEvidenceId = FindAddedId(Workspace.Evidence.Select(item => item.Id), updatedWorkspace.Evidence.Select(item => item.Id));
            SetWorkspace(updatedWorkspace);
            SelectEvidence(createdEvidenceId);
            RegisterRecentChange(createdEvidenceId, "Evidence");
            EnqueueToast("Evidence Added", "Captured a new evidence item.", "Success", "Evidence");
            CloseSpotlightComposer();
            StatusMessage = "Added evidence.";
            return;
        }

        if (Evidence.SelectedEvidence is null)
        {
            throw new InvalidOperationException("Select evidence to update or click New Evidence to add new evidence.");
        }

        var evidenceId = Evidence.SelectedEvidence.Id;
        SetWorkspace(_workspaceService.UpdateEvidence(Workspace, evidenceId, Evidence.EvidenceTitle, Evidence.EvidenceCitation, Evidence.EvidenceNotes, confidence));
        RegisterRecentChange(evidenceId, "Evidence");
        EnqueueToast("Evidence Updated", "Saved the selected evidence item.", "Success", "Evidence");
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

    private void SaveEvidenceAssessment()
    {
        if (Evidence.SelectedEvidence is null)
        {
            throw new InvalidOperationException("Select an evidence item first.");
        }

        if (Evidence.SelectedTargetKind is null || Evidence.SelectedTarget is null || Evidence.SelectedRelation is null || Evidence.SelectedStrength is null)
        {
            throw new InvalidOperationException("Select a target, relation, and strength.");
        }

        var confidence = ParseOptionalConfidence(Evidence.LinkConfidenceText, "Evidence assessment confidence");
        if (Evidence.SelectedLink is null)
        {
            var updatedWorkspace = _workspaceService.AddEvidenceAssessment(
                Workspace,
                Evidence.SelectedEvidence.Id,
                Evidence.SelectedTargetKind.Kind,
                Evidence.SelectedTarget.Id,
                Evidence.SelectedRelation.Relation,
                Evidence.SelectedStrength.Strength,
                Evidence.LinkNotes,
                confidence);
            var createdLinkId = FindAddedId(Workspace.EvidenceLinks.Select(item => item.Id), updatedWorkspace.EvidenceLinks.Select(item => item.Id));
            SetWorkspace(updatedWorkspace);
            RegisterRecentChange(createdLinkId, "Evidence Assessment");
            EnqueueToast("Assessment Added", "Attached a new evidence assessment.", "Success", "Evidence Assessment");
            StatusMessage = "Added evidence assessment.";
            return;
        }

        var linkId = Evidence.SelectedLink.Id;
        SetWorkspace(_workspaceService.UpdateEvidenceAssessment(
            Workspace,
            linkId,
            Evidence.SelectedEvidence.Id,
            Evidence.SelectedTargetKind.Kind,
            Evidence.SelectedTarget.Id,
            Evidence.SelectedRelation.Relation,
            Evidence.SelectedStrength.Strength,
            Evidence.LinkNotes,
            confidence));
        RegisterRecentChange(linkId, "Evidence Assessment");
        EnqueueToast("Assessment Updated", "Saved the selected evidence assessment.", "Success", "Evidence Assessment");
        StatusMessage = "Updated selected evidence assessment.";
    }

    private void DeleteSelectedEvidenceAssessment()
    {
        if (Evidence.SelectedLink is null)
        {
            throw new InvalidOperationException("Select an evidence assessment to delete.");
        }

        SetWorkspace(_workspaceService.RemoveEvidenceAssessment(Workspace, Evidence.SelectedLink.Id));
        Evidence.BeginNewAssessment();
        StatusMessage = "Deleted selected evidence assessment.";
    }

    private void OpenSpotlight(SpotlightComposerMode mode)
    {
        SpotlightMode = mode;
        IsSpotlightComposerOpen = mode != SpotlightComposerMode.None;
        IsRightDrawerOpen = true;
    }

    private void CloseSpotlightComposer()
    {
        IsSpotlightComposerOpen = false;
        SpotlightMode = SpotlightComposerMode.None;
    }

    private void RegisterRecentChange(Guid itemId, string itemKind)
    {
        RecentlyChangedItemId = itemId;
        RecentlyChangedItemKind = itemKind;
    }

    private void EnqueueToast(string title, string message, string tone, string? itemKind = null)
    {
        Toasts.Insert(0, new ShellToastViewModel(title, message, tone, itemKind));
        while (Toasts.Count > 4)
        {
            Toasts.RemoveAt(Toasts.Count - 1);
        }

        OnPropertyChanged(nameof(HasToasts));
    }

    private void SetWorkspace(AnalysisWorkspace workspace)
    {
        Workspace = workspace;
        WorkspaceName = workspace.Name;
        RefreshWorkspaceState();
    }

    private void SelectEvent(Guid id) =>
        Events.SelectedEvent = Events.Events.FirstOrDefault(item => item.Id == id);

    private void SelectClaim(Guid id) =>
        Claims.SelectedClaim = Claims.Claims.FirstOrDefault(item => item.Id == id);

    private void SelectHypothesis(Guid id) =>
        Hypotheses.SelectedHypothesis = Hypotheses.Hypotheses.FirstOrDefault(item => item.Id == id);

    private void SelectEvidence(Guid id) =>
        Evidence.SelectedEvidence = Evidence.EvidenceItems.FirstOrDefault(item => item.Id == id);

    private void RefreshWorkspaceState()
    {
        var selectedEntityId = Entities.SelectedEntity?.Id;
        var selectedRelationshipId = Relationships.SelectedRelationship?.Id;
        var selectedEventId = Events.SelectedEvent?.Id;
        var selectedClaimId = Claims.SelectedClaim?.Id;
        var selectedHypothesisId = Hypotheses.SelectedHypothesis?.Id;
        var selectedEvidenceId = Evidence.SelectedEvidence?.Id;
        var selectedLinkId = Evidence.SelectedLink?.Id;
        var selectedSourceId = Relationships.SelectedSource?.Id;
        var selectedTargetId = Relationships.SelectedTarget?.Id;
        var selectedLink = selectedLinkId is null ? null : Workspace.EvidenceLinks.FirstOrDefault(link => link.Id == selectedLinkId);
        var targetKind = selectedLink?.TargetKind ?? Evidence.SelectedTargetKind?.Kind ?? EvidenceLinkTargetKind.Entity;

        _analysis = _analysisService.SummarizeAsync(Workspace).GetAwaiter().GetResult();

        Overview.Refresh(Workspace.Name, _analysis);
        Findings.Refresh(_analysis);

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
                relationship.SourceEntityId,
                relationship.TargetEntityId,
                ResolveEntityName(relationship.SourceEntityId),
                ResolveEntityName(relationship.TargetEntityId),
                relationship.RelationshipType,
                relationship.Notes,
                relationship.Confidence))
            .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var eventItems = Workspace.Events
            .Select(@event => new EventSummaryViewModel(
                @event.Id,
                @event.Title,
                @event.OccurredAtUtc,
                @event.Notes,
                @event.Confidence,
                BuildEventParticipantRoleGroups(@event.Id)))
            .ToArray();

        var claimItems = Workspace.Claims
            .OrderByDescending(claim => claim.Status == ClaimStatus.Active)
            .ThenBy(claim => claim.Statement, StringComparer.OrdinalIgnoreCase)
            .Select(claim => new ClaimSummaryViewModel(
                claim.Id,
                claim.Statement,
                claim.ClaimType,
                claim.Status,
                claim.Confidence,
                claim.Notes,
                claim.TargetKind,
                claim.TargetId,
                claim.HypothesisId))
            .ToArray();

        var hypothesisItems = Workspace.Hypotheses
            .OrderBy(hypothesis => hypothesis.Title, StringComparer.OrdinalIgnoreCase)
            .Select(hypothesis => new HypothesisSummaryViewModel(hypothesis.Id, hypothesis.Title, hypothesis.Statement, hypothesis.Status, hypothesis.Confidence, hypothesis.Notes))
            .ToArray();

        var evidenceItems = Workspace.Evidence
            .OrderBy(evidence => evidence.Title, StringComparer.OrdinalIgnoreCase)
            .Select(evidence => new EvidenceSummaryViewModel(evidence.Id, evidence.Title, evidence.Citation, evidence.Notes, evidence.Confidence))
            .ToArray();

        Entities.Refresh(entityItems, selectedEntityId);
        Relationships.Refresh(relationshipItems, entityOptions, selectedRelationshipId, selectedSourceId, selectedTargetId);
        Events.Refresh(eventItems, selectedEventId);
        Claims.Refresh(claimItems, BuildClaimTargetOptions(Claims.SelectedTargetKind?.Kind), hypothesisItems, selectedClaimId);
        Hypotheses.Refresh(hypothesisItems, selectedHypothesisId);
        Evidence.Refresh(evidenceItems, selectedEvidenceId, BuildEvidenceLinkSummaries(selectedEvidenceId), CreateTargetKindOptions(), BuildTargetOptions(targetKind), selectedLinkId);

        RefreshEntityLinkedEvidence();
        RefreshRelationshipLinkedEvidence();
        RefreshEventLinkedEvidence();
        RefreshEventParticipants();
        RefreshClaimLinkedEvidence();
        RefreshHypothesisEvidence();
        RefreshEvidenceAssessmentsForSelection();
        RefreshEvidenceTargets(targetKind);
        Claims.UpdateTargets(BuildClaimTargetOptions(Claims.SelectedTargetKind?.Kind));

        RefreshInsightPulseItems();

        OnPropertyChanged(nameof(WorkspaceEntityCount));
        OnPropertyChanged(nameof(WorkspaceRelationshipCount));
        OnPropertyChanged(nameof(WorkspaceEventCount));
        OnPropertyChanged(nameof(WorkspaceParticipantCount));
        OnPropertyChanged(nameof(WorkspaceClaimCount));
        OnPropertyChanged(nameof(WorkspaceHypothesisCount));
        OnPropertyChanged(nameof(WorkspaceEvidenceCount));
        OnPropertyChanged(nameof(WorkspaceEvidenceLinkCount));
        OnPropertyChanged(nameof(CurrentDrawerTitle));
        OnPropertyChanged(nameof(CurrentDrawerHint));
    }

    private void RefreshInsightPulseItems()
    {
        var items = new List<InsightPulseItemViewModel>();

        foreach (var finding in _analysis.Findings
                     .OrderByDescending(item => item.PriorityScore)
                     .Take(3))
        {
            items.Add(new InsightPulseItemViewModel(
                finding.Title,
                finding.Detail,
                finding.Severity.ToString(),
                ResolveTargetSection(finding),
                true));
        }

        if (items.Count == 0 && WorkspaceEntityCount == 0)
        {
            items.Add(new InsightPulseItemViewModel(
                "Start With Entities",
                "Capture the primary people, organizations, and assets to anchor the workspace.",
                "Info",
                WorkbenchSection.Entities,
                true));
        }

        if (items.Count < 3 && WorkspaceEventCount == 0 && WorkspaceClaimCount + WorkspaceEvidenceCount > 0)
        {
            items.Add(new InsightPulseItemViewModel(
                "Build Chronology",
                "There is evidence and narrative data but no dated events yet.",
                "Warning",
                WorkbenchSection.Events,
                true));
        }

        if (items.Count < 3 && WorkspaceHypothesisCount == 0 && WorkspaceClaimCount > 0)
        {
            items.Add(new InsightPulseItemViewModel(
                "Promote Claims Into Hypotheses",
                "Working claims exist without explicit competing explanations.",
                "Info",
                WorkbenchSection.Hypotheses,
                true));
        }

        ReplaceCollection(InsightPulseItems, items.Take(4).ToArray());
        OnPropertyChanged(nameof(HasInsightPulseItems));
    }

    private static WorkbenchSection ResolveTargetSection(AnalysisFinding finding) =>
        finding.TargetKind switch
        {
            "Entity" => WorkbenchSection.Entities,
            "Relationship" => WorkbenchSection.Relationships,
            "Event" => WorkbenchSection.Events,
            "Claim" => WorkbenchSection.Claims,
            "Hypothesis" => WorkbenchSection.Hypotheses,
            "Evidence" => WorkbenchSection.Evidence,
            _ => finding.Category switch
            {
                FindingCategory.SupportGap => WorkbenchSection.Findings,
                FindingCategory.Contradiction => WorkbenchSection.Claims,
                FindingCategory.Timeline => WorkbenchSection.Events,
                FindingCategory.Hypothesis => WorkbenchSection.Hypotheses,
                FindingCategory.Collection => WorkbenchSection.Findings,
                FindingCategory.Participation => WorkbenchSection.Events,
                _ => WorkbenchSection.Overview
            }
        };

    private void RefreshEntityLinkedEvidence() =>
        Entities.UpdateLinkedEvidence(Entities.SelectedEntity is null
            ? []
            : BuildLinkedEvidence(_workspaceService.GetLinkedEvidenceByTarget(Workspace, EvidenceLinkTargetKind.Entity, Entities.SelectedEntity.Id)));

    private void RefreshRelationshipLinkedEvidence() =>
        Relationships.UpdateLinkedEvidence(Relationships.SelectedRelationship is null
            ? []
            : BuildLinkedEvidence(_workspaceService.GetLinkedEvidenceByTarget(Workspace, EvidenceLinkTargetKind.Relationship, Relationships.SelectedRelationship.Id)));

    private void RefreshEventLinkedEvidence() =>
        Events.UpdateLinkedEvidence(Events.SelectedEvent is null
            ? []
            : BuildLinkedEvidence(_workspaceService.GetLinkedEvidenceByTarget(Workspace, EvidenceLinkTargetKind.Event, Events.SelectedEvent.Id)));

    private void RefreshEventParticipants()
    {
        if (Events.SelectedEvent is null)
        {
            Events.UpdateParticipants([], BuildEntityOptions());
            return;
        }

        Events.UpdateParticipants(
            _workspaceService.GetParticipantsForEvent(Workspace, Events.SelectedEvent.Id)
                .Select(participant => new EventParticipantSummaryViewModel(
                    participant.Id,
                    participant.EntityId,
                    ResolveEntityDisplayName(participant.EntityId),
                    participant.Role,
                    participant.Confidence,
                    participant.Notes))
                .ToArray(),
            BuildEntityOptions());
    }

    private IReadOnlyList<EventParticipantRoleGroupViewModel> BuildEventParticipantRoleGroups(Guid eventId) =>
        Workspace.EventParticipants
            .Where(participant => participant.EventId == eventId)
            .GroupBy(participant => participant.Role.Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new EventParticipantRoleGroupViewModel(
                group.First().Role.Trim(),
                group.Select(participant => ResolveEntityDisplayName(participant.EntityId))
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToArray()))
            .ToArray();

    private void RefreshClaimLinkedEvidence() =>
        Claims.UpdateLinkedEvidence(Claims.SelectedClaim is null
            ? []
            : BuildLinkedEvidence(_workspaceService.GetLinkedEvidenceByTarget(Workspace, EvidenceLinkTargetKind.Claim, Claims.SelectedClaim.Id)));

    private void RefreshHypothesisEvidence()
    {
        if (Hypotheses.SelectedHypothesis is null)
        {
            Hypotheses.UpdateEvidence([], [], "No hypothesis selected.", "Inference summaries will appear here.");
            return;
        }

        var summary = _analysis.HypothesisSummaries.FirstOrDefault(item => item.HypothesisId == Hypotheses.SelectedHypothesis.Id);
        if (summary is null)
        {
            Hypotheses.UpdateEvidence([], [], "Unassessed", "No evidence assessments have been attached yet.");
            return;
        }

        Hypotheses.UpdateEvidence(
            summary.SupportingEvidence.Select(ToLinkedEvidenceViewModel).ToArray(),
            summary.ContradictingEvidence.Select(ToLinkedEvidenceViewModel).ToArray(),
            summary.Posture,
            summary.Explanation);
    }

    private void RefreshEvidenceAssessmentsForSelection() =>
        Evidence.UpdateLinkedTargets(BuildEvidenceLinkSummaries(Evidence.SelectedEvidence?.Id));

    private void RefreshEvidenceTargets(EvidenceLinkTargetKind targetKind) =>
        Evidence.UpdateTargets(BuildTargetOptions(targetKind));

    private IReadOnlyList<LinkedEvidenceSummaryViewModel> BuildLinkedEvidence(IReadOnlyList<LinkedEvidenceSummary> linkedEvidence) =>
        linkedEvidence.Select(item => new LinkedEvidenceSummaryViewModel(
            item.EvidenceLinkId,
            item.Title,
            item.Citation,
            item.RelationToTarget,
            item.Strength,
            item.LinkNotes,
            item.LinkConfidence)).ToArray();

    private static LinkedEvidenceSummaryViewModel ToLinkedEvidenceViewModel(HypothesisEvidenceLine item) =>
        new(
            item.EvidenceAssessmentId,
            item.EvidenceTitle,
            item.Citation,
            item.RelationToTarget,
            item.Strength,
            item.Notes,
            item.Weight);

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
                link.EvidenceId,
                Workspace.Evidence.First(evidence => evidence.Id == link.EvidenceId).Title,
                link.TargetKind,
                link.TargetId,
                ResolveTargetDisplay(link.TargetKind, link.TargetId),
                link.RelationToTarget,
                link.Strength,
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
            EvidenceLinkTargetKind.Hypothesis => Workspace.Hypotheses
                .OrderBy(hypothesis => hypothesis.Title, StringComparer.OrdinalIgnoreCase)
                .Select(hypothesis => new TargetOptionViewModel(hypothesis.Id, hypothesis.Title))
                .ToArray(),
            EvidenceLinkTargetKind.Claim => Workspace.Claims
                .OrderBy(claim => claim.Statement, StringComparer.OrdinalIgnoreCase)
                .Select(claim => new TargetOptionViewModel(claim.Id, claim.Statement))
                .ToArray(),
            _ => []
        };

    private IReadOnlyList<TargetOptionViewModel> BuildClaimTargetOptions(ClaimTargetKind? targetKind) =>
        targetKind switch
        {
            ClaimTargetKind.Entity => Workspace.Entities
                .OrderBy(entity => entity.Name, StringComparer.OrdinalIgnoreCase)
                .Select(entity => new TargetOptionViewModel(entity.Id, $"{entity.Name} ({entity.EntityType})"))
                .ToArray(),
            ClaimTargetKind.Relationship => Workspace.Relationships
                .Select(relationship => new TargetOptionViewModel(
                    relationship.Id,
                    $"{ResolveEntityName(relationship.SourceEntityId)} -> {relationship.RelationshipType} -> {ResolveEntityName(relationship.TargetEntityId)}"))
                .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            ClaimTargetKind.Event => Workspace.Events
                .OrderBy(@event => @event.OccurredAtUtc ?? DateTimeOffset.MaxValue)
                .ThenBy(@event => @event.Title, StringComparer.OrdinalIgnoreCase)
                .Select(@event => new TargetOptionViewModel(@event.Id, @event.Title))
                .ToArray(),
            ClaimTargetKind.Hypothesis => Workspace.Hypotheses
                .OrderBy(hypothesis => hypothesis.Title, StringComparer.OrdinalIgnoreCase)
                .Select(hypothesis => new TargetOptionViewModel(hypothesis.Id, hypothesis.Title))
                .ToArray(),
            _ => []
        };

    private IReadOnlyList<EntityOptionViewModel> BuildEntityOptions() =>
        Workspace.Entities
            .OrderBy(entity => entity.Name, StringComparer.OrdinalIgnoreCase)
            .Select(entity => new EntityOptionViewModel(entity.Id, $"{entity.Name} ({entity.EntityType})"))
            .ToArray();

    private static IReadOnlyList<EvidenceLinkTargetKindOptionViewModel> CreateTargetKindOptions() =>
    [
        new(EvidenceLinkTargetKind.Entity, "Entity"),
        new(EvidenceLinkTargetKind.Relationship, "Relationship"),
        new(EvidenceLinkTargetKind.Event, "Event"),
        new(EvidenceLinkTargetKind.Hypothesis, "Hypothesis"),
        new(EvidenceLinkTargetKind.Claim, "Claim")
    ];

    private string ResolveEntityName(Guid entityId) =>
        Workspace.Entities.FirstOrDefault(entity => entity.Id == entityId)?.Name ?? entityId.ToString();

    private string ResolveEntityDisplayName(Guid entityId) =>
        Workspace.Entities.FirstOrDefault(entity => entity.Id == entityId)?.DisplayName() ?? entityId.ToString();

    private string ResolveTargetDisplay(EvidenceLinkTargetKind targetKind, Guid targetId) =>
        targetKind switch
        {
            EvidenceLinkTargetKind.Entity => Workspace.Entities.FirstOrDefault(entity => entity.Id == targetId)?.DisplayName() ?? targetId.ToString(),
            EvidenceLinkTargetKind.Relationship => Workspace.Relationships
                .Where(relationship => relationship.Id == targetId)
                .Select(relationship => $"{ResolveEntityName(relationship.SourceEntityId)} -> {relationship.RelationshipType} -> {ResolveEntityName(relationship.TargetEntityId)}")
                .FirstOrDefault() ?? targetId.ToString(),
            EvidenceLinkTargetKind.Event => Workspace.Events.FirstOrDefault(@event => @event.Id == targetId)?.Title ?? targetId.ToString(),
            EvidenceLinkTargetKind.Hypothesis => Workspace.Hypotheses.FirstOrDefault(hypothesis => hypothesis.Id == targetId)?.Title ?? targetId.ToString(),
            EvidenceLinkTargetKind.Claim => Workspace.Claims.FirstOrDefault(claim => claim.Id == targetId)?.Statement ?? targetId.ToString(),
            _ => targetId.ToString()
        };

    private string BuildSiblingArtifactPath(string suffix)
    {
        var path = WorkspacePath.Trim();
        var directory = Path.GetDirectoryName(path);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
        var resolvedFileName = string.IsNullOrWhiteSpace(fileNameWithoutExtension)
            ? $"{Workspace.Name}{suffix}"
            : $"{fileNameWithoutExtension}{suffix}";
        return string.IsNullOrWhiteSpace(directory)
            ? resolvedFileName
            : Path.Combine(directory, resolvedFileName);
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

    private async Task<string?> EnsureWorkspacePathForSaveAsync()
    {
        if (!string.IsNullOrWhiteSpace(WorkspacePath))
        {
            return WorkspacePath.Trim();
        }

        var suggestedFileName = BuildWorkspaceFileName();
        var selectedPath = await _workspaceFileDialogService.PickSaveWorkspacePathAsync(suggestedFileName);
        return string.IsNullOrWhiteSpace(selectedPath) ? null : selectedPath.Trim();
    }

    private string BuildWorkspaceFileName()
    {
        var baseName = string.IsNullOrWhiteSpace(WorkspaceName) ? "workspace" : WorkspaceName.Trim();
        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
        {
            baseName = baseName.Replace(invalidCharacter, '_');
        }

        if (!baseName.EndsWith(".ifn.json", StringComparison.OrdinalIgnoreCase))
        {
            baseName += ".ifn.json";
        }

        return baseName;
    }

    private static Guid FindAddedId(IEnumerable<Guid> existingIds, IEnumerable<Guid> updatedIds)
    {
        var existing = existingIds.ToHashSet();
        return updatedIds.First(id => !existing.Contains(id));
    }

    private static void ReplaceCollection<T>(ObservableCollection<T> collection, IReadOnlyList<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
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
