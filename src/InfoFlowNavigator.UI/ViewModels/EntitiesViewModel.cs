using System.Collections.ObjectModel;
using System.Windows.Input;

namespace InfoFlowNavigator.UI.ViewModels;

public sealed class EntitiesViewModel : ViewModelBase
{
    private readonly Action<EntitySummaryViewModel?> _selectionChanged;
    private string _newEntityName = string.Empty;
    private string _newEntityType = "Person";
    private EntitySummaryViewModel? _selectedEntity;
    private string _editorName = string.Empty;
    private string _editorType = string.Empty;
    private string _editorNotes = string.Empty;
    private string _editorConfidenceText = string.Empty;

    public EntitiesViewModel(
        ICommand addEntityCommand,
        ICommand saveEntityCommand,
        ICommand deleteEntityCommand,
        Action<EntitySummaryViewModel?> selectionChanged)
    {
        AddEntityCommand = addEntityCommand;
        SaveEntityCommand = saveEntityCommand;
        DeleteEntityCommand = deleteEntityCommand;
        _selectionChanged = selectionChanged;
    }

    public ObservableCollection<EntitySummaryViewModel> Entities { get; } = [];

    public ObservableCollection<LinkedEvidenceSummaryViewModel> LinkedEvidence { get; } = [];

    public ICommand AddEntityCommand { get; }

    public ICommand SaveEntityCommand { get; }

    public ICommand DeleteEntityCommand { get; }

    public string NewEntityName
    {
        get => _newEntityName;
        set => SetProperty(ref _newEntityName, value);
    }

    public string NewEntityType
    {
        get => _newEntityType;
        set => SetProperty(ref _newEntityType, value);
    }

    public EntitySummaryViewModel? SelectedEntity
    {
        get => _selectedEntity;
        set
        {
            if (SetProperty(ref _selectedEntity, value))
            {
                PopulateEditor(value);
                _selectionChanged(value);
                OnPropertyChanged(nameof(HasSelection));
                OnPropertyChanged(nameof(NoSelection));
                OnPropertyChanged(nameof(InspectorTitle));
                OnPropertyChanged(nameof(InspectorHint));
            }
        }
    }

    public string EditorName
    {
        get => _editorName;
        set => SetProperty(ref _editorName, value);
    }

    public string EditorType
    {
        get => _editorType;
        set => SetProperty(ref _editorType, value);
    }

    public string EditorNotes
    {
        get => _editorNotes;
        set => SetProperty(ref _editorNotes, value);
    }

    public string EditorConfidenceText
    {
        get => _editorConfidenceText;
        set => SetProperty(ref _editorConfidenceText, value);
    }

    public bool HasSelection => SelectedEntity is not null;

    public bool NoSelection => !HasSelection;

    public bool IsEmpty => Entities.Count == 0;

    public string InspectorTitle => SelectedEntity is null ? "Entity Inspector" : SelectedEntity.DisplayName;

    public string InspectorHint => SelectedEntity is null
        ? "Select an entity to edit details and review linked evidence."
        : "Edit the selected entity and review the evidence currently attached to it.";

    public void Refresh(IReadOnlyList<EntitySummaryViewModel> entities, Guid? selectedEntityId)
    {
        ReplaceCollection(Entities, entities);
        SelectedEntity = selectedEntityId is null ? null : Entities.FirstOrDefault(entity => entity.Id == selectedEntityId);
        if (SelectedEntity is null)
        {
            ClearEditor();
        }

        OnPropertyChanged(nameof(IsEmpty));
    }

    public void UpdateLinkedEvidence(IReadOnlyList<LinkedEvidenceSummaryViewModel> linkedEvidence) =>
        ReplaceCollection(LinkedEvidence, linkedEvidence);

    public void ClearAddForm()
    {
        NewEntityName = string.Empty;
        NewEntityType = "Person";
    }

    private void PopulateEditor(EntitySummaryViewModel? entity)
    {
        if (entity is null)
        {
            ClearEditor();
            return;
        }

        EditorName = entity.Name;
        EditorType = entity.EntityType;
        EditorNotes = entity.Notes ?? string.Empty;
        EditorConfidenceText = entity.Confidence?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private void ClearEditor()
    {
        EditorName = string.Empty;
        EditorType = string.Empty;
        EditorNotes = string.Empty;
        EditorConfidenceText = string.Empty;
    }

    private static void ReplaceCollection<T>(ObservableCollection<T> collection, IReadOnlyList<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}
