using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;

namespace InfoFlowNavigator.UI.ViewModels;

public sealed class RelationshipsViewModel : ViewModelBase
{
    private const string DefaultRelationshipType = "associated_with";
    private readonly Action<RelationshipSummaryViewModel?> _selectionChanged;
    private bool _isPopulatingEditor;
    private string _relationshipType = DefaultRelationshipType;
    private string _relationshipNotes = string.Empty;
    private string _relationshipConfidenceText = string.Empty;
    private RelationshipSummaryViewModel? _selectedRelationship;
    private IReadOnlyList<string> _relationshipTypeSuggestions = [DefaultRelationshipType];

    public RelationshipsViewModel(
        ICommand saveRelationshipCommand,
        ICommand deleteRelationshipCommand,
        Action<RelationshipSummaryViewModel?> selectionChanged)
    {
        SaveRelationshipCommand = saveRelationshipCommand;
        DeleteRelationshipCommand = deleteRelationshipCommand;
        _selectionChanged = selectionChanged;
        SourcePicker = new SearchSelectionViewModel<EntityOptionViewModel>(
            item => item.DisplayName,
            _ => OnPropertyChanged(nameof(SelectedSource)));
        TargetPicker = new SearchSelectionViewModel<EntityOptionViewModel>(
            item => item.DisplayName,
            _ => OnPropertyChanged(nameof(SelectedTarget)));
    }

    public ObservableCollection<EntityOptionViewModel> EntityOptions { get; } = [];

    public ObservableCollection<RelationshipSummaryViewModel> Relationships { get; } = [];

    public ObservableCollection<LinkedEvidenceSummaryViewModel> LinkedEvidence { get; } = [];

    public IReadOnlyList<string> RelationshipTypeSuggestions
    {
        get => _relationshipTypeSuggestions;
        private set => SetProperty(ref _relationshipTypeSuggestions, value);
    }

    public SearchSelectionViewModel<EntityOptionViewModel> SourcePicker { get; }

    public SearchSelectionViewModel<EntityOptionViewModel> TargetPicker { get; }

    public ICommand SaveRelationshipCommand { get; }

    public ICommand DeleteRelationshipCommand { get; }

    public EntityOptionViewModel? SelectedSource
    {
        get => SourcePicker.SelectedItem;
        set => SourcePicker.SelectedItem = value;
    }

    public EntityOptionViewModel? SelectedTarget
    {
        get => TargetPicker.SelectedItem;
        set => TargetPicker.SelectedItem = value;
    }

    public string RelationshipType
    {
        get => _relationshipType;
        set => SetProperty(ref _relationshipType, value);
    }

    public string RelationshipNotes
    {
        get => _relationshipNotes;
        set => SetProperty(ref _relationshipNotes, value);
    }

    public string RelationshipConfidenceText
    {
        get => _relationshipConfidenceText;
        set => SetProperty(ref _relationshipConfidenceText, value);
    }

    public RelationshipSummaryViewModel? SelectedRelationship
    {
        get => _selectedRelationship;
        set
        {
            if (SetProperty(ref _selectedRelationship, value))
            {
                PopulateEditor(value);
                _selectionChanged(value);
                OnPropertyChanged(nameof(HasSelection));
                OnPropertyChanged(nameof(NoSelection));
                OnPropertyChanged(nameof(InspectorTitle));
                OnPropertyChanged(nameof(InspectorHint));
                OnPropertyChanged(nameof(PrimaryActionLabel));
            }
        }
    }

    public bool HasSelection => SelectedRelationship is not null;

    public bool NoSelection => !HasSelection;

    public bool IsEmpty => Relationships.Count == 0;

    public string InspectorTitle => SelectedRelationship is null ? "Relationship Editor" : "Edit Relationship";

    public string InspectorHint => SelectedRelationship is null
        ? "Capture the connection between two entities, then review linked evidence here."
        : SelectedRelationship.DisplayName;

    public string PrimaryActionLabel => SelectedRelationship is null ? "Add Relationship" : "Update Relationship";

    public void Refresh(
        IReadOnlyList<RelationshipSummaryViewModel> relationships,
        IReadOnlyList<EntityOptionViewModel> entityOptions,
        Guid? selectedRelationshipId,
        Guid? selectedSourceId,
        Guid? selectedTargetId)
    {
        ReplaceCollection(Relationships, relationships);
        ReplaceCollection(EntityOptions, entityOptions);
        SourcePicker.ReplaceItems(EntityOptions, SelectedSource);
        TargetPicker.ReplaceItems(EntityOptions, SelectedTarget);

        _isPopulatingEditor = true;
        try
        {
            SelectedRelationship = selectedRelationshipId is null
                ? null
                : Relationships.FirstOrDefault(relationship => relationship.Id == selectedRelationshipId);

            if (SelectedRelationship is null)
            {
                SelectedSource = ResolveEntityOption(selectedSourceId);
                SelectedTarget = ResolveEntityOption(selectedTargetId);
                RelationshipType = string.IsNullOrWhiteSpace(RelationshipType) ? DefaultRelationshipType : RelationshipType;
                RelationshipNotes = string.Empty;
                RelationshipConfidenceText = string.Empty;
            }
        }
        finally
        {
            _isPopulatingEditor = false;
        }

        if (SelectedRelationship is not null)
        {
            PopulateEditor(SelectedRelationship);
        }

        OnPropertyChanged(nameof(IsEmpty));
    }

    public void UpdateLinkedEvidence(IReadOnlyList<LinkedEvidenceSummaryViewModel> linkedEvidence) =>
        ReplaceCollection(LinkedEvidence, linkedEvidence);

    public void UpdateRelationshipTypeSuggestions(IReadOnlyList<string> suggestions) =>
        RelationshipTypeSuggestions = [.. suggestions
            .Append(DefaultRelationshipType)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)];

    public void BeginNewRelationship()
    {
        _isPopulatingEditor = true;
        try
        {
            SelectedRelationship = null;
            SelectedSource ??= EntityOptions.FirstOrDefault();
            SelectedTarget ??= EntityOptions.FirstOrDefault();
            RelationshipType = DefaultRelationshipType;
            RelationshipNotes = string.Empty;
            RelationshipConfidenceText = string.Empty;
        }
        finally
        {
            _isPopulatingEditor = false;
        }

        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(NoSelection));
        OnPropertyChanged(nameof(InspectorTitle));
        OnPropertyChanged(nameof(InspectorHint));
        OnPropertyChanged(nameof(PrimaryActionLabel));
    }

    private void PopulateEditor(RelationshipSummaryViewModel? relationship)
    {
        if (_isPopulatingEditor)
        {
            return;
        }

        _isPopulatingEditor = true;
        try
        {
            if (relationship is null)
            {
                RelationshipNotes = string.Empty;
                RelationshipConfidenceText = string.Empty;
                return;
            }

            SelectedSource = ResolveEntityOption(relationship.SourceEntityId);
            SelectedTarget = ResolveEntityOption(relationship.TargetEntityId);
            RelationshipType = relationship.RelationshipType;
            RelationshipNotes = relationship.Notes ?? string.Empty;
            RelationshipConfidenceText = relationship.Confidence?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        }
        finally
        {
            _isPopulatingEditor = false;
        }
    }

    private EntityOptionViewModel? ResolveEntityOption(Guid? entityId) =>
        entityId is null
            ? EntityOptions.FirstOrDefault()
            : EntityOptions.FirstOrDefault(item => item.Id == entityId) ?? EntityOptions.FirstOrDefault();

    private static void ReplaceCollection<T>(ObservableCollection<T> collection, IReadOnlyList<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}
