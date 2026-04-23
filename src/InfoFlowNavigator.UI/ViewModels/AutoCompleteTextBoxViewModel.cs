using System.Collections.ObjectModel;

namespace InfoFlowNavigator.UI.ViewModels;

public sealed class AutoCompleteTextBoxViewModel : ViewModelBase
{
    private List<string> _allSuggestions = [];
    private string _text = string.Empty;
    private string _placeholderText = string.Empty;
    private string? _selectedSuggestion;
    private bool _isDropDownOpen;
    private bool _hasFocus;

    public string Text
    {
        get => _text;
        set
        {
            if (!SetProperty(ref _text, value))
            {
                return;
            }

            if (_selectedSuggestion is not null &&
                !string.Equals(_selectedSuggestion, value, StringComparison.OrdinalIgnoreCase))
            {
                _selectedSuggestion = null;
                OnPropertyChanged(nameof(SelectedSuggestion));
                OnPropertyChanged(nameof(HasActiveSuggestion));
            }

            UpdateFilter();
        }
    }

    public string PlaceholderText
    {
        get => _placeholderText;
        set => SetProperty(ref _placeholderText, value);
    }

    public List<string> AllSuggestions
    {
        get => _allSuggestions;
        set
        {
            _allSuggestions = NormalizeSuggestions(value);
            OnPropertyChanged();
            UpdateFilter();
        }
    }

    public ObservableCollection<string> FilteredSuggestions { get; } = [];

    public string? SelectedSuggestion
    {
        get => _selectedSuggestion;
        set
        {
            if (SetProperty(ref _selectedSuggestion, value))
            {
                OnPropertyChanged(nameof(HasActiveSuggestion));
            }
        }
    }

    public bool IsDropDownOpen
    {
        get => _isDropDownOpen;
        set => SetProperty(ref _isDropDownOpen, value);
    }

    public bool HasActiveSuggestion => SelectedSuggestion is not null;

    public void SetFocusState(bool hasFocus)
    {
        _hasFocus = hasFocus;
        UpdateDropDownVisibility();
    }

    public void MoveSelection(int delta)
    {
        if (FilteredSuggestions.Count == 0)
        {
            return;
        }

        var currentIndex = SelectedSuggestion is null
            ? (delta > 0 ? -1 : 0)
            : FilteredSuggestions.IndexOf(SelectedSuggestion);
        var nextIndex = currentIndex + delta;

        if (nextIndex < 0)
        {
            nextIndex = FilteredSuggestions.Count - 1;
        }
        else if (nextIndex >= FilteredSuggestions.Count)
        {
            nextIndex = 0;
        }

        SelectedSuggestion = FilteredSuggestions[nextIndex];
        IsDropDownOpen = true;
    }

    public bool AcceptSelection()
    {
        if (SelectedSuggestion is null)
        {
            return false;
        }

        Text = SelectedSuggestion;
        IsDropDownOpen = false;
        return true;
    }

    public void CloseDropDown() => IsDropDownOpen = false;

    private void UpdateFilter()
    {
        IEnumerable<string> filtered = AllSuggestions;
        if (!string.IsNullOrWhiteSpace(Text))
        {
            filtered = filtered.Where(item => item.StartsWith(Text, StringComparison.OrdinalIgnoreCase));
        }

        ReplaceCollection(FilteredSuggestions, filtered.ToArray());

        if (SelectedSuggestion is not null &&
            !FilteredSuggestions.Contains(SelectedSuggestion, StringComparer.OrdinalIgnoreCase))
        {
            _selectedSuggestion = null;
            OnPropertyChanged(nameof(SelectedSuggestion));
            OnPropertyChanged(nameof(HasActiveSuggestion));
        }

        OnPropertyChanged(nameof(FilteredSuggestions));
        UpdateDropDownVisibility();
    }

    private void UpdateDropDownVisibility() =>
        IsDropDownOpen = _hasFocus && FilteredSuggestions.Count > 0;

    private static List<string> NormalizeSuggestions(IEnumerable<string>? suggestions) =>
        suggestions?
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

    private static void ReplaceCollection<T>(ObservableCollection<T> collection, IReadOnlyList<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}
