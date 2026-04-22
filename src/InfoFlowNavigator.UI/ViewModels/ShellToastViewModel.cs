namespace InfoFlowNavigator.UI.ViewModels;

public sealed class ShellToastViewModel : ViewModelBase
{
    private bool _isVisible = true;

    public ShellToastViewModel(
        string title,
        string message,
        string tone,
        string? itemKind = null)
    {
        Id = Guid.NewGuid();
        Title = title;
        Message = message;
        Tone = tone;
        ItemKind = itemKind;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }

    public string Title { get; }

    public string Message { get; }

    public string Tone { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public string? ItemKind { get; }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }
}
