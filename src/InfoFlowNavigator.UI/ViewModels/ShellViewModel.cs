using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using InfoFlowNavigator.Application.Workspaces;
using InfoFlowNavigator.Domain.Workspaces;

namespace InfoFlowNavigator.UI.ViewModels;

public sealed class ShellViewModel : INotifyPropertyChanged
{
    private readonly WorkspaceApplicationService _workspaceService;
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
    private string _statusMessage;

    public ShellViewModel(WorkspaceApplicationService workspaceService)
    {
        _workspaceService = workspaceService;
        _workspace = workspaceService.CreateWorkspace("Untitled Workspace");
        _statusMessage = "Create or open a workspace to begin.";

        Entities = new ObservableCollection<EntitySummaryViewModel>();
        Relationships = new ObservableCollection<RelationshipSummaryViewModel>();
        EvidenceItems = new ObservableCollection<EvidenceSummaryViewModel>();
        RelationshipEntityOptions = new ObservableCollection<EntityOptionViewModel>();

        WorkspaceName = _workspace.Name;
        RefreshWorkspaceState();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Title => "Info Flow Navigator";

    public string Subtitle => "Analyst workspace slice";

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
        set => SetField(ref _selectedRelationship, value);
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
                OnPropertyChanged(nameof(WorkspaceEvidenceCount));
                OnPropertyChanged(nameof(WorkspaceCreatedAt));
                OnPropertyChanged(nameof(WorkspaceUpdatedAt));
                OnPropertyChanged(nameof(WorkspaceId));
            }
        }
    }

    public int WorkspaceEntityCount => Workspace.Entities.Count;

    public int WorkspaceRelationshipCount => Workspace.Relationships.Count;

    public int WorkspaceEvidenceCount => Workspace.Evidence.Count;

    public string WorkspaceId => Workspace.Id.ToString();

    public string WorkspaceCreatedAt => Workspace.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");

    public string WorkspaceUpdatedAt => Workspace.UpdatedAtUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");

    public ObservableCollection<EntitySummaryViewModel> Entities { get; }

    public ObservableCollection<RelationshipSummaryViewModel> Relationships { get; }

    public ObservableCollection<EvidenceSummaryViewModel> EvidenceItems { get; }

    public ObservableCollection<EntityOptionViewModel> RelationshipEntityOptions { get; }

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

        SetWorkspace(
            _workspaceService.UpdateEntity(
                Workspace,
                SelectedEntity.Id,
                EditableEntityName,
                EditableEntityType,
                EditableEntityNotes,
                confidence));

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

        SetWorkspace(
            _workspaceService.AddRelationship(
                Workspace,
                SelectedSourceEntity.Id,
                SelectedTargetEntity.Id,
                RelationshipType));

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

        SetWorkspace(
            _workspaceService.UpdateEvidence(
                Workspace,
                SelectedEvidence.Id,
                EvidenceTitle,
                EvidenceCitation,
                EvidenceNotes,
                confidence));

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
            var source = Workspace.Entities.FirstOrDefault(entity => entity.Id == relationship.SourceEntityId)?.Name ?? relationship.SourceEntityId.ToString();
            var target = Workspace.Entities.FirstOrDefault(entity => entity.Id == relationship.TargetEntityId)?.Name ?? relationship.TargetEntityId.ToString();
            Relationships.Add(new RelationshipSummaryViewModel(relationship.Id, source, target, relationship.RelationshipType, relationship.Notes, relationship.Confidence));
        }

        EvidenceItems.Clear();
        foreach (var evidence in Workspace.Evidence)
        {
            EvidenceItems.Add(new EvidenceSummaryViewModel(evidence.Id, evidence.Title, evidence.Citation, evidence.Notes, evidence.Confidence));
        }

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

        OnPropertyChanged(nameof(WorkspaceEntityCount));
        OnPropertyChanged(nameof(WorkspaceRelationshipCount));
        OnPropertyChanged(nameof(WorkspaceEvidenceCount));
        OnPropertyChanged(nameof(WorkspaceCreatedAt));
        OnPropertyChanged(nameof(WorkspaceUpdatedAt));
        OnPropertyChanged(nameof(WorkspaceId));
        OnPropertyChanged(nameof(EvidenceActionLabel));
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

public sealed record EntitySummaryViewModel(Guid Id, string Name, string EntityType, string? Notes, double? Confidence)
{
    public string DisplayName => $"{Name} ({EntityType})";
}

public sealed record EntityOptionViewModel(Guid Id, string DisplayName);

public sealed record RelationshipSummaryViewModel(
    Guid Id,
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
