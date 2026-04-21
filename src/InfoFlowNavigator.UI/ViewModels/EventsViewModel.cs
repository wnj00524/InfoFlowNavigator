using System.Collections.ObjectModel;
using System.Windows.Input;

namespace InfoFlowNavigator.UI.ViewModels;

public sealed class EventsViewModel : EditorWorkflowViewModel
{
    private readonly Action<EventSummaryViewModel?> _selectionChanged;
    private EventSummaryViewModel? _selectedEvent;
    private EventParticipantSummaryViewModel? _selectedParticipant;
    private EntityOptionViewModel? _selectedParticipantEntity;
    private string _eventTitle = string.Empty;
    private string _eventOccurredAtText = string.Empty;
    private string _eventNotes = string.Empty;
    private string _eventConfidenceText = string.Empty;
    private string _participantRole = string.Empty;
    private string _participantConfidenceText = string.Empty;
    private string _participantNotes = string.Empty;

    public EventsViewModel(
        ICommand beginNewEventCommand,
        ICommand saveEventCommand,
        ICommand deleteEventCommand,
        ICommand addParticipantCommand,
        ICommand removeParticipantCommand,
        Action<EventSummaryViewModel?> selectionChanged)
    {
        BeginNewEventCommand = beginNewEventCommand;
        SaveEventCommand = saveEventCommand;
        DeleteEventCommand = deleteEventCommand;
        AddParticipantCommand = addParticipantCommand;
        RemoveParticipantCommand = removeParticipantCommand;
        _selectionChanged = selectionChanged;
    }

    protected override string ItemTypeDisplayName => "Event";

    protected override string CreateHintText => "Record observed activity, meetings, milestones, or other dated developments.";

    protected override string EditHintText => "Update the selected event and review its supporting evidence.";

    public ObservableCollection<EventSummaryViewModel> Events { get; } = [];

    public ObservableCollection<LinkedEvidenceSummaryViewModel> LinkedEvidence { get; } = [];

    public ObservableCollection<EventParticipantSummaryViewModel> Participants { get; } = [];

    public ObservableCollection<EntityOptionViewModel> ParticipantEntities { get; } = [];

    public ICommand BeginNewEventCommand { get; }

    public ICommand SaveEventCommand { get; }

    public ICommand DeleteEventCommand { get; }

    public ICommand AddParticipantCommand { get; }

    public ICommand RemoveParticipantCommand { get; }

    public EventSummaryViewModel? SelectedEvent
    {
        get => _selectedEvent;
        set => SetEditorSelection(ref _selectedEvent, value, _selectionChanged, PopulateEditor, ClearEditor, nameof(SelectedEvent));
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

    public EventParticipantSummaryViewModel? SelectedParticipant
    {
        get => _selectedParticipant;
        set
        {
            if (SetProperty(ref _selectedParticipant, value))
            {
                PopulateParticipantEditor(value);
            }
        }
    }

    public EntityOptionViewModel? SelectedParticipantEntity
    {
        get => _selectedParticipantEntity;
        set => SetProperty(ref _selectedParticipantEntity, value);
    }

    public string ParticipantRole
    {
        get => _participantRole;
        set => SetProperty(ref _participantRole, value);
    }

    public string ParticipantConfidenceText
    {
        get => _participantConfidenceText;
        set => SetProperty(ref _participantConfidenceText, value);
    }

    public string ParticipantNotes
    {
        get => _participantNotes;
        set => SetProperty(ref _participantNotes, value);
    }

    public bool IsEmpty => Events.Count == 0;

    public void BeginNewEvent()
    {
        EnterAddMode();
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

    public void UpdateParticipants(IReadOnlyList<EventParticipantSummaryViewModel> participants, IReadOnlyList<EntityOptionViewModel> entities)
    {
        var selectedParticipantId = SelectedParticipant?.Id;
        ReplaceCollection(Participants, participants);
        ReplaceCollection(ParticipantEntities, entities);
        SelectedParticipant = selectedParticipantId is null ? null : Participants.FirstOrDefault(item => item.Id == selectedParticipantId);
        if (SelectedParticipant is null)
        {
            ClearParticipantEditor();
        }
    }

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

    private void PopulateParticipantEditor(EventParticipantSummaryViewModel? participant)
    {
        if (participant is null)
        {
            ClearParticipantEditor();
            return;
        }

        SelectedParticipantEntity = ParticipantEntities.FirstOrDefault(item => item.Id == participant.EntityId);
        ParticipantRole = participant.Role;
        ParticipantConfidenceText = participant.Confidence?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
        ParticipantNotes = participant.Notes ?? string.Empty;
    }

    public void ClearParticipantEditor()
    {
        SelectedParticipantEntity = null;
        ParticipantRole = string.Empty;
        ParticipantConfidenceText = string.Empty;
        ParticipantNotes = string.Empty;
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
