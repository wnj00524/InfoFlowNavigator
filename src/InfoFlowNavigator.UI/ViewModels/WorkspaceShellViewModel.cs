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
    private AnalysisWorkspace _workspace;
    private WorkspaceAnalysisResult _analysis;
    private string _workspaceName = string.Empty;
    private string _workspacePath = "workspace.ifn.json";
    private string _statusMessage;
    private WorkbenchSectionItemViewModel? _selectedSection;
    private string? _lastNetworkExportPath;

    public WorkspaceShellViewModel(
        WorkspaceApplicationService workspaceService,
        IAnalysisService analysisService,
        IReportGenerator reportGenerator,
        IWorkspaceExportService workspaceExportService)
    {
        _workspaceService = workspaceService;
        _analysisService = analysisService;
        _reportGenerator = reportGenerator;
        _workspaceExportService = workspaceExportService;
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
            new RelayCommand(() => ExecuteSafely(SaveHypothesis)),
            new RelayCommand(() => ExecuteSafely(DeleteSelectedHypothesis)),
            _ => RefreshHypothesisEvidence());
        Evidence = new EvidenceViewModel(
            new RelayCommand(() => ExecuteSafely(SaveEvidence)),
            new RelayCommand(() => ExecuteSafely(DeleteSelectedEvidence)),
            new RelayCommand(() => ExecuteSafely(AddEvidenceAssessment)),
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
        SetWorkspace(_workspaceService.CreateWorkspace(name));
        StatusMessage = "Created a new workspace.";
    }

    private async Task OpenWorkspaceAsync()
    {
        EnsurePathProvided();
        _lastNetworkExportPath = null;
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

    private async Task ExportBriefingAsync()
    {
        EnsurePathProvided();
        var artifact = await _reportGenerator.GenerateAsync(Workspace);
        var outputPath = BuildSiblingArtifactPath(".briefing.txt");
        var content = artifact.Content;
        if (!string.IsNullOrWhiteSpace(_lastNetworkExportPath))
        {
            content += $"{Environment.NewLine}Network Export Note{Environment.NewLine}- Last network export: {_lastNetworkExportPath}{Environment.NewLine}";
        }

        await File.WriteAllTextAsync(outputPath, content);
        StatusMessage = $"Exported analyst briefing to '{outputPath}'.";
    }

    private async Task ExportNetworkAsync()
    {
        EnsurePathProvided();
        var outputPath = BuildSiblingArtifactPath(".network.json");
        await _workspaceExportService.ExportAsync(Workspace, outputPath);
        _lastNetworkExportPath = outputPath;
        StatusMessage = $"Exported MedWNetwork-compatible network JSON to '{outputPath}'.";
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
        SetWorkspace(_workspaceService.UpdateEntity(Workspace, Entities.SelectedEntity.Id, Entities.EditorName, Entities.EditorType, Entities.EditorNotes, confidence));
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

        SetWorkspace(_workspaceService.AddRelationship(Workspace, Relationships.SelectedSource.Id, Relationships.SelectedTarget.Id, Relationships.RelationshipType));
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

        SetWorkspace(_workspaceService.UpdateEvent(Workspace, Events.SelectedEvent.Id, Events.EventTitle, occurredAtUtc, Events.EventNotes, confidence));
        StatusMessage = "Updated selected event.";
    }

    private void BeginNewEvent()
    {
        Events.BeginNewEvent();
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
            throw new InvalidOperationException("Select an event first.");
        }

        if (Events.SelectedParticipantEntity is null)
        {
            throw new InvalidOperationException("Select an entity for the participant row.");
        }

        var confidence = ParseOptionalConfidence(Events.ParticipantConfidenceText, "Participant confidence");
        if (Events.SelectedParticipant is null)
        {
            SetWorkspace(_workspaceService.AddEventParticipant(
                Workspace,
                Events.SelectedEvent.Id,
                Events.SelectedParticipantEntity.Id,
                Events.ParticipantRole,
                confidence,
                Events.ParticipantNotes));
            Events.ClearParticipantEditor();
            StatusMessage = "Added event participant.";
            return;
        }

        SetWorkspace(_workspaceService.UpdateEventParticipant(
            Workspace,
            Events.SelectedParticipant.Id,
            Events.ParticipantRole,
            confidence,
            Events.ParticipantNotes));
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
        Claims.BeginNewClaim();
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

        if (Claims.SelectedClaim is null)
        {
            SetWorkspace(_workspaceService.AddClaim(
                Workspace,
                Claims.Statement,
                claimType,
                claimStatus,
                confidence,
                Claims.Notes,
                targetKind,
                targetId,
                hypothesisId));
            StatusMessage = "Added claim to the workspace.";
            return;
        }

        SetWorkspace(_workspaceService.UpdateClaim(
            Workspace,
            Claims.SelectedClaim.Id,
            Claims.Statement,
            claimType,
            claimStatus,
            confidence,
            Claims.Notes,
            targetKind,
            targetId,
            hypothesisId));
        StatusMessage = "Updated selected claim.";
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
        var confidence = ParseOptionalConfidence(Hypotheses.ConfidenceText, "Hypothesis confidence");
        var status = Hypotheses.SelectedStatus?.Status ?? HypothesisStatus.Draft;

        if (Hypotheses.SelectedHypothesis is null)
        {
            SetWorkspace(_workspaceService.AddHypothesis(Workspace, Hypotheses.Title, Hypotheses.Statement, status, confidence, Hypotheses.Notes));
            StatusMessage = "Added hypothesis to the workspace.";
            return;
        }

        SetWorkspace(_workspaceService.UpdateHypothesis(Workspace, Hypotheses.SelectedHypothesis.Id, Hypotheses.Title, Hypotheses.Statement, status, confidence, Hypotheses.Notes));
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

    private void SaveEvidence()
    {
        var confidence = ParseOptionalConfidence(Evidence.EvidenceConfidenceText, "Evidence confidence");

        if (Evidence.SelectedEvidence is null)
        {
            SetWorkspace(_workspaceService.AddEvidence(Workspace, Evidence.EvidenceTitle, Evidence.EvidenceCitation, Evidence.EvidenceNotes, confidence));
            StatusMessage = "Added evidence to the workspace.";
            return;
        }

        SetWorkspace(_workspaceService.UpdateEvidence(Workspace, Evidence.SelectedEvidence.Id, Evidence.EvidenceTitle, Evidence.EvidenceCitation, Evidence.EvidenceNotes, confidence));
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

    private void AddEvidenceAssessment()
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
        SetWorkspace(_workspaceService.AddEvidenceAssessment(
            Workspace,
            Evidence.SelectedEvidence.Id,
            Evidence.SelectedTargetKind.Kind,
            Evidence.SelectedTarget.Id,
            Evidence.SelectedRelation.Relation,
            Evidence.SelectedStrength.Strength,
            Evidence.LinkNotes,
            confidence));
        StatusMessage = "Added evidence assessment.";
    }

    private void DeleteSelectedEvidenceAssessment()
    {
        if (Evidence.SelectedLink is null)
        {
            throw new InvalidOperationException("Select an evidence assessment to delete.");
        }

        SetWorkspace(_workspaceService.RemoveEvidenceAssessment(Workspace, Evidence.SelectedLink.Id));
        StatusMessage = "Deleted selected evidence assessment.";
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
        var selectedClaimId = Claims.SelectedClaim?.Id;
        var selectedHypothesisId = Hypotheses.SelectedHypothesis?.Id;
        var selectedEvidenceId = Evidence.SelectedEvidence?.Id;
        var selectedLinkId = Evidence.SelectedLink?.Id;
        var selectedSourceId = Relationships.SelectedSource?.Id;
        var selectedTargetId = Relationships.SelectedTarget?.Id;
        var targetKind = Evidence.SelectedTargetKind?.Kind ?? EvidenceLinkTargetKind.Entity;

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

        OnPropertyChanged(nameof(WorkspaceEntityCount));
        OnPropertyChanged(nameof(WorkspaceRelationshipCount));
        OnPropertyChanged(nameof(WorkspaceEventCount));
        OnPropertyChanged(nameof(WorkspaceParticipantCount));
        OnPropertyChanged(nameof(WorkspaceClaimCount));
        OnPropertyChanged(nameof(WorkspaceHypothesisCount));
        OnPropertyChanged(nameof(WorkspaceEvidenceCount));
        OnPropertyChanged(nameof(WorkspaceEvidenceLinkCount));
    }

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
                Workspace.Evidence.First(evidence => evidence.Id == link.EvidenceId).Title,
                link.TargetKind,
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
