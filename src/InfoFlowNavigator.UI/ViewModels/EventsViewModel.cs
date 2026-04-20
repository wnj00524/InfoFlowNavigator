using System.Collections.ObjectModel;
using System.Windows.Input;

namespace InfoFlowNavigator.UI.ViewModels;

public sealed class EventsViewModel : ViewModelBase
{
    private readonly Action<EventSummaryViewModel?> _selectionChanged;
    private EventSummaryViewModel? _selectedEvent;
    private string _eventTitle = string.Empty;
    private string _eventOccurredAtText = string.Empty;
    private string _eventNotes = string.Empty;
    private string _eventConfidenceText = string.Empty;

    public EventsViewModel(
        ICommand beginNewEventCommand,
        ICommand saveEventCommand,
        ICommand deleteEventCommand,
        Action<EventSummaryViewModel?> selectionChanged)
    {
        BeginNewEventCommand = beginNewEventCommand;
        SaveEventCommand = saveEventCommand;
        DeleteEventCommand = deleteEventCommand;
        _selectionChanged = selectionChanged;
    }

    public ObservableCollection<EventSummaryViewModel> Events { get; } = [];

    public ObservableCollection<LinkedEvidenceSummaryViewModel> LinkedEvidence { get; } = [];

    public ICommand BeginNewEventCommand { get; }

    public ICommand SaveEventCommand { get; }

    public ICommand DeleteEventCommand { get; }

    public EventSummaryViewModel? SelectedEvent
    {
        get => _selectedEvent;
        set
        {
            if (SetProperty(ref _selectedEvent, value))
            {
                PopulateEditor(value);
                _selectionChanged(value);
                OnPropertyChanged(nameof(HasSelection));
                OnPropertyChanged(nameof(NoSelection));
                OnPropertyChanged(nameof(EditorTitle));
                OnPropertyChanged(nameof(EditorHint));
                OnPropertyChanged(nameof(PrimaryActionLabel));
            }
        }
    }

    public string EventTitle
    {
        get => _eventTitle;
        set => SetProperty(ref _eventTitle, value);
    }

    public string EventOccurredAtText
    {
        get => _eventOccurredAtText;
        set => SetProperty(ref _eventOccurredAtText, value);
    }

    public string EventNotes
    {
        get => _eventNotes;
        set => SetProperty(ref _eventNotes, value);
    }

    public string EventConfidenceText
    {
        get => _eventConfidenceText;
        set => SetProperty(ref _eventConfidenceText, value);
    }

    public bool HasSelection => SelectedEvent is not null;

    public bool NoSelection => !HasSelection;

    public bool IsEmpty => Events.Count == 0;

    public string EditorTitle => SelectedEvent is null ? "Create Event" : "Edit Event";

    public string EditorHint => SelectedEvent is null
        ? "Record observed activity, meetings, milestones, or other dated developments."
        : "Update the selected event and review its supporting evidence.";

    public string PrimaryActionLabel => SelectedEvent is null ? "Add Event" : "Update Event";

    public void BeginNewEvent()
    {
        SelectedEvent = null;
        ClearEditor();
    }

    public void Refresh(IReadOnlyList<EventSummaryViewModel> events, Guid? selectedEventId)
    {
        ReplaceCollection(Events, events.OrderBy(item => item.OccurredAtUtc ?? DateTimeOffset.MaxValue).ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase).ToArray());
        SelectedEvent = selectedEventId is null ? null : Events.FirstOrDefault(@event => @event.Id == selectedEventId);
        if (SelectedEvent is null)
        {
            ClearEditor();
        }

        OnPropertyChanged(nameof(IsEmpty));
    }

    public void UpdateLinkedEvidence(IReadOnlyList<LinkedEvidenceSummaryViewModel> linkedEvidence) =>
        ReplaceCollection(LinkedEvidence, linkedEvidence);

    private void PopulateEditor(EventSummaryViewModel? summary)
    {
        if (summary is null)
        {
            ClearEditor();
            return;
        }

        EventTitle = summary.Title;
        EventOccurredAtText = summary.OccurredAtUtc?.ToString("O", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
        EventNotes = summary.Notes ?? string.Empty;
        EventConfidenceText = summary.Confidence?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private void ClearEditor()
    {
        EventTitle = string.Empty;
        EventOccurredAtText = string.Empty;
        EventNotes = string.Empty;
        EventConfidenceText = string.Empty;
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
