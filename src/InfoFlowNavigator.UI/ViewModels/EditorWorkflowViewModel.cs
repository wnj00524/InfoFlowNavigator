namespace InfoFlowNavigator.UI.ViewModels;

public abstract class EditorWorkflowViewModel : ViewModelBase
{
    private bool _isEditingExistingItem;

    protected abstract string ItemTypeDisplayName { get; }

    protected abstract string CreateHintText { get; }

    protected abstract string EditHintText { get; }

    public bool HasSelection => _isEditingExistingItem;

    public bool NoSelection => !_isEditingExistingItem;

    public bool IsEditingExistingItem => _isEditingExistingItem;

    public string EditorTitle => _isEditingExistingItem ? $"Edit {ItemTypeDisplayName}" : $"Create {ItemTypeDisplayName}";

    public string EditorHint => _isEditingExistingItem ? EditHintText : CreateHintText;

    public string PrimaryActionLabel => _isEditingExistingItem ? $"Save {ItemTypeDisplayName}" : $"Add {ItemTypeDisplayName}";

    protected void EnterAddMode()
    {
        _isEditingExistingItem = false;
        RaiseEditorStateChanged();
    }

    protected bool SetEditorSelection<TSelection>(
        ref TSelection? field,
        TSelection? value,
        Action<TSelection?> selectionChanged,
        Action<TSelection> populateEditor,
        Action clearEditor,
        string propertyName) where TSelection : class
    {
        if (!SetProperty(ref field, value, propertyName))
        {
            return false;
        }

        _isEditingExistingItem = value is not null;
        if (value is null)
        {
            clearEditor();
        }
        else
        {
            populateEditor(value);
        }

        selectionChanged(value);
        RaiseEditorStateChanged();
        return true;
    }

    protected void RaiseEditorStateChanged()
    {
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(NoSelection));
        OnPropertyChanged(nameof(IsEditingExistingItem));
        OnPropertyChanged(nameof(EditorTitle));
        OnPropertyChanged(nameof(EditorHint));
        OnPropertyChanged(nameof(PrimaryActionLabel));
    }
}
