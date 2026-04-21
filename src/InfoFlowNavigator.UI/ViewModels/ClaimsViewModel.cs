using System.Collections.ObjectModel;
using System.Windows.Input;
using InfoFlowNavigator.Domain.Claims;

namespace InfoFlowNavigator.UI.ViewModels;

public sealed class ClaimsViewModel : ViewModelBase
{
    private readonly Action<ClaimSummaryViewModel?> _selectionChanged;
    private ClaimSummaryViewModel? _selectedClaim;
    private ClaimTypeOptionViewModel? _selectedClaimType;
    private ClaimStatusOptionViewModel? _selectedClaimStatus;
    private ClaimTargetKindOptionViewModel? _selectedTargetKind;
    private TargetOptionViewModel? _selectedTarget;
    private HypothesisSummaryViewModel? _selectedHypothesis;
    private string _statement = string.Empty;
    private string _notes = string.Empty;
    private string _confidenceText = string.Empty;

    public ClaimsViewModel(
        ICommand beginNewClaimCommand,
        ICommand saveClaimCommand,
        ICommand deleteClaimCommand,
        Action<ClaimSummaryViewModel?> selectionChanged)
    {
        BeginNewClaimCommand = beginNewClaimCommand;
        SaveClaimCommand = saveClaimCommand;
        DeleteClaimCommand = deleteClaimCommand;
        _selectionChanged = selectionChanged;
    }

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

    public ICommand BeginNewClaimCommand { get; }

    public ICommand SaveClaimCommand { get; }

    public ICommand DeleteClaimCommand { get; }

    public ClaimSummaryViewModel? SelectedClaim
    {
        get => _selectedClaim;
        set
        {
            if (SetProperty(ref _selectedClaim, value))
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

    public ClaimTypeOptionViewModel? SelectedClaimType
    {
        get => _selectedClaimType;
        set => SetProperty(ref _selectedClaimType, value);
    }

    public ClaimStatusOptionViewModel? SelectedClaimStatus
    {
        get => _selectedClaimStatus;
        set => SetProperty(ref _selectedClaimStatus, value);
    }

    public ClaimTargetKindOptionViewModel? SelectedTargetKind
    {
        get => _selectedTargetKind;
        set
        {
            if (SetProperty(ref _selectedTargetKind, value))
            {
                SelectedTarget = null;
            }
        }
    }

    public TargetOptionViewModel? SelectedTarget
    {
        get => _selectedTarget;
        set => SetProperty(ref _selectedTarget, value);
    }

    public HypothesisSummaryViewModel? SelectedHypothesis
    {
        get => _selectedHypothesis;
        set => SetProperty(ref _selectedHypothesis, value);
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

    public bool HasSelection => SelectedClaim is not null;

    public bool NoSelection => !HasSelection;

    public bool IsEmpty => Claims.Count == 0;

    public string EditorTitle => SelectedClaim is null ? "Create Claim" : "Edit Claim";

    public string EditorHint => SelectedClaim is null
        ? "Capture a specific analytic assertion that sits between evidence and higher-level hypotheses."
        : "Update the claim and review the evidence currently attached to it.";

    public string PrimaryActionLabel => SelectedClaim is null ? "Add Claim" : "Update Claim";

    public void BeginNewClaim()
    {
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
        SelectedClaim = selectedClaimId is null ? null : Claims.FirstOrDefault(item => item.Id == selectedClaimId);
        if (SelectedClaim is null)
        {
            ClearEditor();
        }

        OnPropertyChanged(nameof(IsEmpty));
    }

    public void UpdateTargets(IReadOnlyList<TargetOptionViewModel> targets) =>
        ReplaceCollection(Targets, targets);

    public void UpdateLinkedEvidence(IReadOnlyList<LinkedEvidenceSummaryViewModel> linkedEvidence) =>
        ReplaceCollection(LinkedEvidence, linkedEvidence);

    private void PopulateEditor(ClaimSummaryViewModel? summary)
    {
        if (summary is null)
        {
            ClearEditor();
            return;
        }

        Statement = summary.Statement;
        Notes = summary.Notes ?? string.Empty;
        ConfidenceText = summary.Confidence?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
        SelectedClaimType = ClaimTypes.FirstOrDefault(item => item.ClaimType == summary.ClaimType);
        SelectedClaimStatus = ClaimStatuses.FirstOrDefault(item => item.Status == summary.Status);
        SelectedTargetKind = TargetKinds.FirstOrDefault(item => item.Kind == summary.TargetKind) ?? TargetKinds[0];
        SelectedTarget = summary.TargetId is null ? null : Targets.FirstOrDefault(item => item.Id == summary.TargetId);
        SelectedHypothesis = summary.HypothesisId is null ? null : Hypotheses.FirstOrDefault(item => item.Id == summary.HypothesisId);
    }

    private void ClearEditor()
    {
        Statement = string.Empty;
        Notes = string.Empty;
        ConfidenceText = string.Empty;
        SelectedClaimType = ClaimTypes[0];
        SelectedClaimStatus = ClaimStatuses[0];
        SelectedTargetKind = TargetKinds[0];
        SelectedTarget = null;
        SelectedHypothesis = null;
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
