using System.Collections.ObjectModel;
using System.Windows.Input;

namespace InfoFlowNavigator.UI.ViewModels;

public sealed class HypothesesViewModel : EditorWorkflowViewModel
{
    private readonly Action<HypothesisSummaryViewModel?> _selectionChanged;
    private HypothesisSummaryViewModel? _selectedHypothesis;
    private HypothesisStatusOptionViewModel? _selectedStatus;
    private string _title = string.Empty;
    private string _statement = string.Empty;
    private string _notes = string.Empty;
    private string _confidenceText = string.Empty;
    private string _posture = "No hypothesis selected.";
    private string _explanation = "Inference summaries will appear here.";

    public HypothesesViewModel(
        ICommand beginNewHypothesisCommand,
        ICommand saveHypothesisCommand,
        ICommand deleteHypothesisCommand,
        Action<HypothesisSummaryViewModel?> selectionChanged)
    {
        BeginNewHypothesisCommand = beginNewHypothesisCommand;
        SaveHypothesisCommand = saveHypothesisCommand;
        DeleteHypothesisCommand = deleteHypothesisCommand;
        _selectionChanged = selectionChanged;

        foreach (var status in new[]
                 {
                     new HypothesisStatusOptionViewModel(Domain.Hypotheses.HypothesisStatus.Draft, "Draft"),
                     new HypothesisStatusOptionViewModel(Domain.Hypotheses.HypothesisStatus.Active, "Active"),
                     new HypothesisStatusOptionViewModel(Domain.Hypotheses.HypothesisStatus.Resolved, "Resolved"),
                     new HypothesisStatusOptionViewModel(Domain.Hypotheses.HypothesisStatus.Rejected, "Rejected")
                 })
        {
            StatusOptions.Add(status);
        }

        SelectedStatus = StatusOptions[0];
    }

    protected override string ItemTypeDisplayName => "Hypothesis";

    protected override string CreateHintText => "Capture a testable analytic statement, then attach supporting and contradicting evidence.";

    protected override string EditHintText => "Review the current posture and update the hypothesis statement as the evidence changes.";

    public ObservableCollection<HypothesisSummaryViewModel> Hypotheses { get; } = [];

    public ObservableCollection<HypothesisStatusOptionViewModel> StatusOptions { get; } = [];

    public ObservableCollection<LinkedEvidenceSummaryViewModel> SupportingEvidence { get; } = [];

    public ObservableCollection<LinkedEvidenceSummaryViewModel> ContradictingEvidence { get; } = [];

    public ICommand BeginNewHypothesisCommand { get; }

    public ICommand SaveHypothesisCommand { get; }

    public ICommand DeleteHypothesisCommand { get; }

    public HypothesisSummaryViewModel? SelectedHypothesis
    {
        get => _selectedHypothesis;
        set => SetEditorSelection(ref _selectedHypothesis, value, _selectionChanged, PopulateEditor, ClearEditor, nameof(SelectedHypothesis));
    }

    public HypothesisStatusOptionViewModel? SelectedStatus
    {
        get => _selectedStatus;
        set => SetProperty(ref _selectedStatus, value);
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string Statement
    {
        get => _statement;
        set => SetProperty(ref _statement, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public string ConfidenceText
    {
        get => _confidenceText;
        set => SetProperty(ref _confidenceText, value);
    }

    public string Posture
    {
        get => _posture;
        private set => SetProperty(ref _posture, value);
    }

    public string Explanation
    {
        get => _explanation;
        private set => SetProperty(ref _explanation, value);
    }

    public bool IsEmpty => Hypotheses.Count == 0;

    public void BeginNewHypothesis()
    {
        EnterAddMode();
        SelectedHypothesis = null;
        ClearEditor();
    }

    public void Refresh(
        IReadOnlyList<HypothesisSummaryViewModel> hypotheses,
        Guid? selectedHypothesisId)
    {
        ReplaceCollection(Hypotheses, hypotheses);
        SelectedHypothesis = selectedHypothesisId is null ? null : Hypotheses.FirstOrDefault(item => item.Id == selectedHypothesisId);

        if (SelectedHypothesis is null)
        {
            ClearEditor();
        }

        OnPropertyChanged(nameof(IsEmpty));
    }

    public void UpdateEvidence(
        IReadOnlyList<LinkedEvidenceSummaryViewModel> supportingEvidence,
        IReadOnlyList<LinkedEvidenceSummaryViewModel> contradictingEvidence,
        string posture,
        string explanation)
    {
        ReplaceCollection(SupportingEvidence, supportingEvidence);
        ReplaceCollection(ContradictingEvidence, contradictingEvidence);
        Posture = posture;
        Explanation = explanation;
    }

    private void PopulateEditor(HypothesisSummaryViewModel? hypothesis)
    {
        if (hypothesis is null)
        {
            ClearEditor();
            return;
        }

        Title = hypothesis.Title;
        Statement = hypothesis.Statement;
        Notes = hypothesis.Notes ?? string.Empty;
        ConfidenceText = hypothesis.Confidence?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
        SelectedStatus = StatusOptions.FirstOrDefault(item => item.Status == hypothesis.Status) ?? StatusOptions[0];
    }

    private void ClearEditor()
    {
        Title = string.Empty;
        Statement = string.Empty;
        Notes = string.Empty;
        ConfidenceText = string.Empty;
        SelectedStatus = StatusOptions.FirstOrDefault();
        UpdateEvidence([], [], "No hypothesis selected.", "Inference summaries will appear here.");
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
