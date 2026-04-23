using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using InfoFlowNavigator.UI.ViewModels;
using System.Threading;

namespace InfoFlowNavigator.UI.Controls;

public partial class AutoCompleteTextBox : UserControl
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<AutoCompleteTextBox, string>(nameof(Text), string.Empty, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<IEnumerable<string>> SuggestionsProperty =
        AvaloniaProperty.Register<AutoCompleteTextBox, IEnumerable<string>>(nameof(Suggestions), []);

    public static readonly StyledProperty<string> PlaceholderTextProperty =
        AvaloniaProperty.Register<AutoCompleteTextBox, string>(nameof(PlaceholderText), string.Empty);

    private readonly AutoCompleteTextBoxViewModel _viewModel = new();
    private CancellationTokenSource? _lostFocusCancellation;

    public AutoCompleteTextBox()
    {
        InitializeComponent();
        DataContext = _viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel.Text = Text ?? string.Empty;
        _viewModel.AllSuggestions = Suggestions?.ToList() ?? [];
        _viewModel.PlaceholderText = PlaceholderText ?? string.Empty;
        DetachedFromVisualTree += OnDetachedFromVisualTree;
    }

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public IEnumerable<string> Suggestions
    {
        get => GetValue(SuggestionsProperty);
        set => SetValue(SuggestionsProperty, value);
    }

    public string PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TextProperty)
        {
            var nextValue = change.GetNewValue<string>() ?? string.Empty;
            if (!string.Equals(_viewModel.Text, nextValue, StringComparison.Ordinal))
            {
                _viewModel.Text = nextValue;
            }
        }
        else if (change.Property == SuggestionsProperty)
        {
            _viewModel.AllSuggestions = change.GetNewValue<IEnumerable<string>>()?.ToList() ?? [];
        }
        else if (change.Property == PlaceholderTextProperty)
        {
            _viewModel.PlaceholderText = change.GetNewValue<string>() ?? string.Empty;
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down:
                _viewModel.MoveSelection(1);
                ScrollSelectionIntoView();
                e.Handled = true;
                break;

            case Key.Up:
                _viewModel.MoveSelection(-1);
                ScrollSelectionIntoView();
                e.Handled = true;
                break;

            case Key.Tab:
                if (_viewModel.AcceptSelection())
                {
                    e.Handled = true;
                }

                break;

            case Key.Enter:
                if (_viewModel.AcceptSelection())
                {
                    e.Handled = true;
                }

                break;

            case Key.Escape:
                _viewModel.CloseDropDown();
                break;
        }
    }

    private void OnFocus(object? sender, RoutedEventArgs e)
    {
        CancelPendingLostFocus();
        _viewModel.SetFocusState(true);
    }

    private async void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        CancelPendingLostFocus();
        var cancellation = new CancellationTokenSource();
        _lostFocusCancellation = cancellation;

        try
        {
            await Task.Delay(120, cancellation.Token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        if (!ReferenceEquals(_lostFocusCancellation, cancellation))
        {
            return;
        }

        _lostFocusCancellation = null;
        if (InputBox?.IsFocused != true)
        {
            _viewModel.SetFocusState(false);
        }
    }

    private void OnSuggestionClicked(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is not StyledElement { DataContext: string suggestion })
        {
            return;
        }

        _viewModel.SelectedSuggestion = suggestion;
        if (_viewModel.AcceptSelection())
        {
            InputBox.Focus();
            e.Handled = true;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AutoCompleteTextBoxViewModel.Text) &&
            !string.Equals(Text, _viewModel.Text, StringComparison.Ordinal))
        {
            SetCurrentValue(TextProperty, _viewModel.Text);
        }
    }

    private void ScrollSelectionIntoView()
    {
        if (_viewModel.SelectedSuggestion is not null)
        {
            SuggestionList.ScrollIntoView(_viewModel.SelectedSuggestion);
        }
    }

    private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        CancelPendingLostFocus();
        _viewModel.SetFocusState(false);
    }

    private void CancelPendingLostFocus()
    {
        _lostFocusCancellation?.Cancel();
        _lostFocusCancellation?.Dispose();
        _lostFocusCancellation = null;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
