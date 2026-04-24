using System.Collections.ObjectModel;

namespace InfoFlowNavigator.UI.ViewModels;

public sealed class SearchSelectionViewModel<T> : ViewModelBase
{
    private readonly Func<T, string> _displaySelector;
    private readonly Action<T?>? _selectionChanged;
    private readonly int _suggestionLimit;
    private bool _areSuggestionsVisible;
    private bool _isUpdatingFromSelection;
    private string _searchText = string.Empty;
    private T? _selectedItem;

    public SearchSelectionViewModel(
        Func<T, string> displaySelector,
        Action<T?>? selectionChanged = null,
        int suggestionLimit = 10)
    {
        _displaySelector = displaySelector;
        _selectionChanged = selectionChanged;
        _suggestionLimit = suggestionLimit;
    }

    public ObservableCollection<T> Items { get; } = [];

    public ObservableCollection<T> Suggestions { get; } = [];

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value))
            {
                return;
            }

            if (_isUpdatingFromSelection)
            {
                return;
            }

            if (_selectedItem is not null &&
                !string.Equals(_displaySelector(_selectedItem), value, StringComparison.OrdinalIgnoreCase))
            {
                _selectedItem = default;
                OnPropertyChanged(nameof(SelectedItem));
                _selectionChanged?.Invoke(default);
            }

            ApplySuggestions();
        }
    }

    public T? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (!SetProperty(ref _selectedItem, value))
            {
                return;
            }

            _isUpdatingFromSelection = true;
            try
            {
                _searchText = value is null ? string.Empty : _displaySelector(value);
                OnPropertyChanged(nameof(SearchText));
            }
            finally
            {
                _isUpdatingFromSelection = false;
            }

            // Keep the backing collection intact during ListBox selection; Avalonia may still read it
            // while committing the selected index.
            HideSuggestions();
            _selectionChanged?.Invoke(value);
        }
    }

    public bool HasSuggestions => _areSuggestionsVisible && Suggestions.Count > 0;

    public void ReplaceItems(IReadOnlyList<T> items, T? selectedItem = default)
    {
        ReplaceCollection(Items, items);

        if (selectedItem is not null)
        {
            SelectedItem = Items.FirstOrDefault(item => EqualityComparer<T>.Default.Equals(item, selectedItem));
            return;
        }

        if (_selectedItem is not null)
        {
            var matched = Items.FirstOrDefault(item => EqualityComparer<T>.Default.Equals(item, _selectedItem));
            if (matched is not null)
            {
                SelectedItem = matched;
                return;
            }
        }

        SelectedItem = default;
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            ApplySuggestions();
        }
    }

    public void Clear()
    {
        SelectedItem = default;
        _isUpdatingFromSelection = true;
        try
        {
            _searchText = string.Empty;
            OnPropertyChanged(nameof(SearchText));
        }
        finally
        {
            _isUpdatingFromSelection = false;
        }

        ClearSuggestions();
    }

    private void ApplySuggestions()
    {
        IEnumerable<T> filtered = Items;
        var query = SearchText.Trim();
        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(item => _displaySelector(item).Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        if (_selectedItem is not null &&
            string.Equals(_displaySelector(_selectedItem), SearchText, StringComparison.OrdinalIgnoreCase))
        {
            HideSuggestions();
            return;
        }

        ReplaceCollection(Suggestions, filtered.Take(_suggestionLimit).ToArray());
        SetSuggestionsVisible(Suggestions.Count > 0);
    }

    private void ClearSuggestions()
    {
        Suggestions.Clear();
        SetSuggestionsVisible(false);
    }

    private void HideSuggestions() => SetSuggestionsVisible(false);

    private void SetSuggestionsVisible(bool value)
    {
        if (_areSuggestionsVisible == value)
        {
            return;
        }

        _areSuggestionsVisible = value;
        OnPropertyChanged(nameof(HasSuggestions));
    }

    private static void ReplaceCollection<TItem>(ObservableCollection<TItem> collection, IReadOnlyList<TItem> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}
