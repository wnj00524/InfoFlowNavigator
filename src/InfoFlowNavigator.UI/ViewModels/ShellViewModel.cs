using System.Collections.ObjectModel;
using System.ComponentModel;
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
    private string _statusMessage;

    public ShellViewModel(WorkspaceApplicationService workspaceService)
    {
        _workspaceService = workspaceService;
        _workspace = workspaceService.CreateWorkspace("Untitled Workspace");
        _statusMessage = "Create or open a workspace to begin.";

        Entities = new ObservableCollection<EntitySummaryViewModel>();
        Relationships = new ObservableCollection<RelationshipSummaryViewModel>();
        RelationshipEntityOptions = new ObservableCollection<EntityOptionViewModel>();

        WorkspaceName = _workspace.Name;
        RefreshWorkspaceState();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Title => "Info Flow Navigator";

    public string Subtitle => "Workspace core slice";

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
                OnPropertyChanged(nameof(WorkspaceCreatedAt));
                OnPropertyChanged(nameof(WorkspaceUpdatedAt));
                OnPropertyChanged(nameof(WorkspaceId));
            }
        }
    }

    public int WorkspaceEntityCount => Workspace.Entities.Count;

    public int WorkspaceRelationshipCount => Workspace.Relationships.Count;

    public string WorkspaceId => Workspace.Id.ToString();

    public string WorkspaceCreatedAt => Workspace.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");

    public string WorkspaceUpdatedAt => Workspace.UpdatedAtUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");

    public ObservableCollection<EntitySummaryViewModel> Entities { get; }

    public ObservableCollection<RelationshipSummaryViewModel> Relationships { get; }

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

    public void SetStatus(string message) => StatusMessage = message;

    private void SetWorkspace(AnalysisWorkspace workspace)
    {
        Workspace = workspace;
        WorkspaceName = workspace.Name;
        RefreshWorkspaceState();
    }

    private void RefreshWorkspaceState()
    {
        Entities.Clear();
        foreach (var entity in Workspace.Entities)
        {
            Entities.Add(new EntitySummaryViewModel(entity.Id, entity.Name, entity.EntityType));
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
            Relationships.Add(new RelationshipSummaryViewModel(relationship.Id, source, target, relationship.RelationshipType));
        }

        if (RelationshipEntityOptions.Count > 0)
        {
            SelectedSourceEntity ??= RelationshipEntityOptions[0];
            SelectedTargetEntity ??= RelationshipEntityOptions[0];
        }
        else
        {
            SelectedSourceEntity = null;
            SelectedTargetEntity = null;
        }

        OnPropertyChanged(nameof(WorkspaceEntityCount));
        OnPropertyChanged(nameof(WorkspaceRelationshipCount));
        OnPropertyChanged(nameof(WorkspaceCreatedAt));
        OnPropertyChanged(nameof(WorkspaceUpdatedAt));
        OnPropertyChanged(nameof(WorkspaceId));
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

public sealed record EntitySummaryViewModel(Guid Id, string Name, string EntityType)
{
    public string DisplayName => $"{Name} ({EntityType})";
}

public sealed record EntityOptionViewModel(Guid Id, string DisplayName);

public sealed record RelationshipSummaryViewModel(Guid Id, string SourceName, string TargetName, string RelationshipType)
{
    public string DisplayName => $"{SourceName} -> {RelationshipType} -> {TargetName}";
}
