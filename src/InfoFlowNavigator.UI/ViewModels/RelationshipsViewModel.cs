using System.Collections.ObjectModel;
using System.Windows.Input;

namespace InfoFlowNavigator.UI.ViewModels;

public sealed class RelationshipsViewModel : ViewModelBase
{
    private readonly Action<RelationshipSummaryViewModel?> _selectionChanged;
    private EntityOptionViewModel? _selectedSource;
    private EntityOptionViewModel? _selectedTarget;
    private string _relationshipType = "associated_with";
    private RelationshipSummaryViewModel? _selectedRelationship;

    public RelationshipsViewModel(
        ICommand addRelationshipCommand,
        ICommand deleteRelationshipCommand,
        Action<RelationshipSummaryViewModel?> selectionChanged)
    {
        AddRelationshipCommand = addRelationshipCommand;
        DeleteRelationshipCommand = deleteRelationshipCommand;
        _selectionChanged = selectionChanged;
    }

    public ObservableCollection<EntityOptionViewModel> EntityOptions { get; } = [];

    public ObservableCollection<RelationshipSummaryViewModel> Relationships { get; } = [];

    public ObservableCollection<LinkedEvidenceSummaryViewModel> LinkedEvidence { get; } = [];

    public ICommand AddRelationshipCommand { get; }

    public ICommand DeleteRelationshipCommand { get; }

    public EntityOptionViewModel? SelectedSource
    {
        get => _selectedSource;
        set => SetProperty(ref _selectedSource, value);
    }

    public EntityOptionViewModel? SelectedTarget
    {
        get => _selectedTarget;
        set => SetProperty(ref _selectedTarget, value);
    }

    public string RelationshipType
    {
        get => _relationshipType;
        set => SetProperty(ref _relationshipType, value);
    }

    public RelationshipSummaryViewModel? SelectedRelationship
    {
        get => _selectedRelationship;
        set
        {
            if (SetProperty(ref _selectedRelationship, value))
            {
                _selectionChanged(value);
                OnPropertyChanged(nameof(HasSelection));
                OnPropertyChanged(nameof(NoSelection));
                OnPropertyChanged(nameof(InspectorTitle));
                OnPropertyChanged(nameof(InspectorHint));
            }
        }
    }

    public bool HasSelection => SelectedRelationship is not null;

    public bool NoSelection => !HasSelection;

    public bool IsEmpty => Relationships.Count == 0;

    public string InspectorTitle => SelectedRelationship is null ? "Relationship Inspector" : "Relationship Detail";

    public string InspectorHint => SelectedRelationship is null
        ? "Select a relationship to review its support."
        : SelectedRelationship.DisplayName;

    public void Refresh(
        IReadOnlyList<RelationshipSummaryViewModel> relationships,
        IReadOnlyList<EntityOptionViewModel> entityOptions,
        Guid? selectedRelationshipId,
        Guid? selectedSourceId,
        Guid? selectedTargetId)
    {
        ReplaceCollection(Relationships, relationships);
        ReplaceCollection(EntityOptions, entityOptions);

        SelectedSource = selectedSourceId is null ? EntityOptions.FirstOrDefault() : EntityOptions.FirstOrDefault(item => item.Id == selectedSourceId) ?? EntityOptions.FirstOrDefault();
        SelectedTarget = selectedTargetId is null ? EntityOptions.FirstOrDefault() : EntityOptions.FirstOrDefault(item => item.Id == selectedTargetId) ?? EntityOptions.FirstOrDefault();
        SelectedRelationship = selectedRelationshipId is null ? null : Relationships.FirstOrDefault(relationship => relationship.Id == selectedRelationshipId);

        OnPropertyChanged(nameof(IsEmpty));
    }

    public void UpdateLinkedEvidence(IReadOnlyList<LinkedEvidenceSummaryViewModel> linkedEvidence) =>
        ReplaceCollection(LinkedEvidence, linkedEvidence);

    private static void ReplaceCollection<T>(ObservableCollection<T> collection, IReadOnlyList<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}
