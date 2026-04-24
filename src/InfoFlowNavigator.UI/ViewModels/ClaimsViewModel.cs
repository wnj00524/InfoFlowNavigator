using System.Collections.ObjectModel;
using System.Windows.Input;
using InfoFlowNavigator.Domain.Claims;

namespace InfoFlowNavigator.UI.ViewModels;

public sealed class ClaimsViewModel : EditorWorkflowViewModel
{
    private readonly Action<ClaimSummaryViewModel?> _selectionChanged;
    private readonly Action<ClaimTargetKind?> _targetKindChanged;
    private bool _isRefreshingEditor;
    private ClaimSummaryViewModel? _selectedClaim;
    private string _statement = string.Empty;
    private string _notes = string.Empty;
    private string _confidenceText = string.Empty;

    public ClaimsViewModel(
        ICommand beginNewClaimCommand,
        ICommand saveClaimCommand,
        ICommand deleteClaimCommand,
        Action<ClaimSummaryViewModel?> selectionChanged,
        Action<ClaimTargetKind?> targetKindChanged)
    {
        BeginNewClaimCommand = beginNewClaimCommand;
        SaveClaimCommand = saveClaimCommand;
        DeleteClaimCommand = deleteClaimCommand;
        _selectionChanged = selectionChanged;
        _targetKindChanged = targetKindChanged;
        ClaimTypePicker = new SearchSelectionViewModel<ClaimTypeOptionViewModel>(
            item => item.DisplayName,
            _ => OnPropertyChanged(nameof(SelectedClaimType)));
        ClaimStatusPicker = new SearchSelectionViewModel<ClaimStatusOptionViewModel>(
            item => item.DisplayName,
            _ => OnPropertyChanged(nameof(SelectedClaimStatus)));
        TargetKindPicker = new SearchSelectionViewModel<ClaimTargetKindOptionViewModel>(
            item => item.DisplayName,
            OnSelectedTargetKindChanged);
        TargetPicker = new SearchSelectionViewModel<TargetOptionViewModel>(
            item => item.DisplayName,
            _ => OnPropertyChanged(nameof(SelectedTarget)));
        HypothesisPicker = new SearchSelectionViewModel<HypothesisSummaryViewModel>(
            item => item.Title,
            _ => OnPropertyChanged(nameof(SelectedHypothesis)));

        ClaimTypePicker.ReplaceItems(ClaimTypes, ClaimTypes[0]);
        ClaimStatusPicker.ReplaceItems(ClaimStatuses, ClaimStatuses[0]);
        TargetKindPicker.ReplaceItems(TargetKinds, TargetKinds[0]);
    }

    protected override string ItemTypeDisplayName => "Claim";

    protected override string CreateHintText => "Capture a specific analytic assertion that sits between evidence and higher-level hypotheses.";

    protected override string EditHintText => "Update the selected claim and review the evidence currently attached to it.";

    public ObservableCollection<ClaimSummaryViewModel> Claims { get; } = [];

    public ObservableCollection<LinkedEvidenceSummaryViewModel> LinkedEvidence { get; } = [];

    public ObservableCollection<ClaimTypeOptionViewModel> ClaimTypes { get; } =
    [
        new(ClaimType.General, "General"),
        new(ClaimType.EventParticipation, "Event Participation"),
        new(ClaimType.Relationship, "Relationship"),
        new(ClaimType.Timeline, "Timeline"),
        new(ClaimType.Activity, "Activity")
    ];

    public ObservableCollection<ClaimStatusOptionViewModel> ClaimStatuses { get; } =
    [
        new(ClaimStatus.Draft, "Draft"),
        new(ClaimStatus.Active, "Active"),
        new(ClaimStatus.Resolved, "Resolved"),
        new(ClaimStatus.Refuted, "Refuted")
    ];

    public ObservableCollection<ClaimTargetKindOptionViewModel> TargetKinds { get; } =
    [
        new(null, "None"),
        new(ClaimTargetKind.Entity, "Entity"),
        new(ClaimTargetKind.Relationship, "Relationship"),
        new(ClaimTargetKind.Event, "Event"),
        new(ClaimTargetKind.Hypothesis, "Hypothesis")
    ];

    public ObservableCollection<TargetOptionViewModel> Targets { get; } = [];

    public ObservableCollection<HypothesisSummaryViewModel> Hypotheses { get; } = [];

    public SearchSelectionViewModel<ClaimTypeOptionViewModel> ClaimTypePicker { get; }

    public SearchSelectionViewModel<ClaimStatusOptionViewModel> ClaimStatusPicker { get; }

    public SearchSelectionViewModel<ClaimTargetKindOptionViewModel> TargetKindPicker { get; }

    public SearchSelectionViewModel<TargetOptionViewModel> TargetPicker { get; }

    public SearchSelectionViewModel<HypothesisSummaryViewModel> HypothesisPicker { get; }

    public ICommand BeginNewClaimCommand { get; }

    public ICommand SaveClaimCommand { get; }

    public ICommand DeleteClaimCommand { get; }

    public ClaimSummaryViewModel? SelectedClaim
    {
        get => _selectedClaim;
        set => SetEditorSelection(ref _selectedClaim, value, _selectionChanged, PopulateEditor, ClearEditor, nameof(SelectedClaim));
    }

    public ClaimTypeOptionViewModel? SelectedClaimType
    {
        get => ClaimTypePicker.SelectedItem;
        set => ClaimTypePicker.SelectedItem = value;
    }

    public ClaimStatusOptionViewModel? SelectedClaimStatus
    {
        get => ClaimStatusPicker.SelectedItem;
        set => ClaimStatusPicker.SelectedItem = value;
    }

    public ClaimTargetKindOptionViewModel? SelectedTargetKind
    {
        get => TargetKindPicker.SelectedItem;
        set => TargetKindPicker.SelectedItem = value;
    }

    public TargetOptionViewModel? SelectedTarget
    {
        get => TargetPicker.SelectedItem;
        set => TargetPicker.SelectedItem = value;
    }

    public HypothesisSummaryViewModel? SelectedHypothesis
    {
        get => HypothesisPicker.SelectedItem;
        set => HypothesisPicker.SelectedItem = value;
    }

    public string Statement
    {
        get => _statement;
        set
        {
            if (SetProperty(ref _statement, value))
            {
                OnPropertyChanged(nameof(CanSaveClaim));
            }
        }
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

    public bool IsEmpty => Claims.Count == 0;

    public bool CanSaveClaim => !string.IsNullOrWhiteSpace(Statement);

    public void BeginNewClaim()
    {
        EnterAddMode();
        SelectedClaim = null;
        ClearEditor();
    }

    public void Refresh(
        IReadOnlyList<ClaimSummaryViewModel> claims,
        IReadOnlyList<TargetOptionViewModel> targets,
        IReadOnlyList<HypothesisSummaryViewModel> hypotheses,
        Guid? selectedClaimId)
    {
        ReplaceCollection(Claims, claims.OrderBy(item => item.Statement, StringComparer.OrdinalIgnoreCase).ToArray());
        ReplaceCollection(Targets, targets);
        ReplaceCollection(Hypotheses, hypotheses);
        ClaimTypePicker.ReplaceItems(ClaimTypes, SelectedClaimType ?? ClaimTypes[0]);
        ClaimStatusPicker.ReplaceItems(ClaimStatuses, SelectedClaimStatus ?? ClaimStatuses[0]);
        TargetKindPicker.ReplaceItems(TargetKinds, SelectedTargetKind ?? TargetKinds[0]);
        TargetPicker.ReplaceItems(Targets, SelectedTarget);
        HypothesisPicker.ReplaceItems(Hypotheses, SelectedHypothesis);
        SelectedClaim = selectedClaimId is null ? null : Claims.FirstOrDefault(item => item.Id == selectedClaimId);
        if (SelectedClaim is null)
        {
            ClearEditor();
        }

        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(CanSaveClaim));
    }

    public void UpdateTargets(IReadOnlyList<TargetOptionViewModel> targets)
    {
        var selectedTargetId = SelectedTarget?.Id;
        ReplaceCollection(Targets, targets);
        var resolvedTarget = selectedTargetId is null
            ? Targets.FirstOrDefault()
            : Targets.FirstOrDefault(item => item.Id == selectedTargetId) ?? Targets.FirstOrDefault();
        TargetPicker.ReplaceItems(Targets, resolvedTarget);
    }

    public void UpdateLinkedEvidence(IReadOnlyList<LinkedEvidenceSummaryViewModel> linkedEvidence) =>
        ReplaceCollection(LinkedEvidence, linkedEvidence);

    private void PopulateEditor(ClaimSummaryViewModel? summary)
    {
        if (summary is null)
        {
            ClearEditor();
            return;
        }

        _isRefreshingEditor = true;
        try
        {
            Statement = summary.Statement;
            Notes = summary.Notes ?? string.Empty;
            ConfidenceText = summary.Confidence?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
            SelectedClaimType = ClaimTypes.FirstOrDefault(item => item.ClaimType == summary.ClaimType);
            SelectedClaimStatus = ClaimStatuses.FirstOrDefault(item => item.Status == summary.Status);
            SelectedTargetKind = TargetKinds.FirstOrDefault(item => item.Kind == summary.TargetKind) ?? TargetKinds[0];
            _targetKindChanged(SelectedTargetKind?.Kind);
            SelectedTarget = summary.TargetId is null ? null : Targets.FirstOrDefault(item => item.Id == summary.TargetId);
            SelectedHypothesis = summary.HypothesisId is null ? null : Hypotheses.FirstOrDefault(item => item.Id == summary.HypothesisId);
        }
        finally
        {
            _isRefreshingEditor = false;
        }
    }

    private void ClearEditor()
    {
        _isRefreshingEditor = true;
        try
        {
            Statement = string.Empty;
            Notes = string.Empty;
            ConfidenceText = string.Empty;
            SelectedClaimType = ClaimTypes[0];
            SelectedClaimStatus = ClaimStatuses[0];
            SelectedTargetKind = TargetKinds[0];
            ReplaceCollection(Targets, []);
            TargetPicker.ReplaceItems(Targets);
            SelectedTarget = null;
            HypothesisPicker.ReplaceItems(Hypotheses);
            SelectedHypothesis = null;
        }
        finally
        {
            _isRefreshingEditor = false;
        }

        _targetKindChanged(SelectedTargetKind?.Kind);
        OnPropertyChanged(nameof(CanSaveClaim));
    }

    private void OnSelectedTargetKindChanged(ClaimTargetKindOptionViewModel? value)
    {
        OnPropertyChanged(nameof(SelectedTargetKind));
        TargetPicker.Clear();
        if (!_isRefreshingEditor)
        {
            _targetKindChanged(value?.Kind);
        }
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
