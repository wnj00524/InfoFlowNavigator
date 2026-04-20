using System.Collections.ObjectModel;
using System.Windows.Input;
using InfoFlowNavigator.Domain.EvidenceLinks;

namespace InfoFlowNavigator.UI.ViewModels;

public sealed class EvidenceViewModel : ViewModelBase
{
    private readonly Action<EvidenceSummaryViewModel?> _selectionChanged;
    private readonly Action<EvidenceLinkTargetKind> _targetKindChanged;
    private EvidenceSummaryViewModel? _selectedEvidence;
    private string _evidenceTitle = string.Empty;
    private string _evidenceCitation = string.Empty;
    private string _evidenceNotes = string.Empty;
    private string _evidenceConfidenceText = string.Empty;
    private EvidenceLinkSummaryViewModel? _selectedLink;
    private EvidenceLinkTargetKindOptionViewModel? _selectedTargetKind;
    private TargetOptionViewModel? _selectedTarget;
    private string _linkRole = string.Empty;
    private string _linkNotes = string.Empty;
    private string _linkConfidenceText = string.Empty;

    public EvidenceViewModel(
        ICommand saveEvidenceCommand,
        ICommand deleteEvidenceCommand,
        ICommand addLinkCommand,
        ICommand deleteLinkCommand,
        Action<EvidenceSummaryViewModel?> selectionChanged,
        Action<EvidenceLinkTargetKind> targetKindChanged)
    {
        SaveEvidenceCommand = saveEvidenceCommand;
        DeleteEvidenceCommand = deleteEvidenceCommand;
        AddLinkCommand = addLinkCommand;
        DeleteLinkCommand = deleteLinkCommand;
        _selectionChanged = selectionChanged;
        _targetKindChanged = targetKindChanged;
    }

    public ObservableCollection<EvidenceSummaryViewModel> EvidenceItems { get; } = [];

    public ObservableCollection<EvidenceLinkSummaryViewModel> LinkedTargets { get; } = [];

    public ObservableCollection<EvidenceLinkTargetKindOptionViewModel> TargetKinds { get; } = [];

    public ObservableCollection<TargetOptionViewModel> Targets { get; } = [];

    public ICommand SaveEvidenceCommand { get; }

    public ICommand DeleteEvidenceCommand { get; }

    public ICommand AddLinkCommand { get; }

    public ICommand DeleteLinkCommand { get; }

    public EvidenceSummaryViewModel? SelectedEvidence
    {
        get => _selectedEvidence;
        set
        {
            if (SetProperty(ref _selectedEvidence, value))
            {
                PopulateEditor(value);
                _selectionChanged(value);
                OnPropertyChanged(nameof(HasSelection));
                OnPropertyChanged(nameof(NoSelection));
                OnPropertyChanged(nameof(EditorTitle));
                OnPropertyChanged(nameof(EditorHint));
                OnPropertyChanged(nameof(PrimaryActionLabel));
                OnPropertyChanged(nameof(LinkHint));
            }
        }
    }

    public string EvidenceTitle
    {
        get => _evidenceTitle;
        set => SetProperty(ref _evidenceTitle, value);
    }

    public string EvidenceCitation
    {
        get => _evidenceCitation;
        set => SetProperty(ref _evidenceCitation, value);
    }

    public string EvidenceNotes
    {
        get => _evidenceNotes;
        set => SetProperty(ref _evidenceNotes, value);
    }

    public string EvidenceConfidenceText
    {
        get => _evidenceConfidenceText;
        set => SetProperty(ref _evidenceConfidenceText, value);
    }

    public EvidenceLinkSummaryViewModel? SelectedLink
    {
        get => _selectedLink;
        set => SetProperty(ref _selectedLink, value);
    }

    public EvidenceLinkTargetKindOptionViewModel? SelectedTargetKind
    {
        get => _selectedTargetKind;
        set
        {
            if (SetProperty(ref _selectedTargetKind, value) && value is not null)
            {
                _targetKindChanged(value.Kind);
            }
        }
    }

    public TargetOptionViewModel? SelectedTarget
    {
        get => _selectedTarget;
        set => SetProperty(ref _selectedTarget, value);
    }

    public string LinkRole
    {
        get => _linkRole;
        set => SetProperty(ref _linkRole, value);
    }

    public string LinkNotes
    {
        get => _linkNotes;
        set => SetProperty(ref _linkNotes, value);
    }

    public string LinkConfidenceText
    {
        get => _linkConfidenceText;
        set => SetProperty(ref _linkConfidenceText, value);
    }

    public bool HasSelection => SelectedEvidence is not null;

    public bool NoSelection => !HasSelection;

    public bool IsEmpty => EvidenceItems.Count == 0;

    public string EditorTitle => SelectedEvidence is null ? "Capture Evidence" : "Edit Evidence";

    public string EditorHint => SelectedEvidence is null
        ? "Capture source material first, then attach it where it supports entities, relationships, or events."
        : "Update the selected evidence and manage where it supports the analysis.";

    public string PrimaryActionLabel => SelectedEvidence is null ? "Add Evidence" : "Update Evidence";

    public string LinkHint => SelectedEvidence is null
        ? "Select an evidence item to add support links."
        : $"Create a support link for '{SelectedEvidence.Title}'.";

    public void Refresh(
        IReadOnlyList<EvidenceSummaryViewModel> evidenceItems,
        Guid? selectedEvidenceId,
        IReadOnlyList<EvidenceLinkSummaryViewModel> linkedTargets,
        IReadOnlyList<EvidenceLinkTargetKindOptionViewModel> targetKinds,
        IReadOnlyList<TargetOptionViewModel> targets,
        Guid? selectedLinkId)
    {
        ReplaceCollection(EvidenceItems, evidenceItems);
        ReplaceCollection(TargetKinds, targetKinds);
        ReplaceCollection(Targets, targets);

        SelectedEvidence = selectedEvidenceId is null ? null : EvidenceItems.FirstOrDefault(item => item.Id == selectedEvidenceId);
        SelectedTargetKind ??= TargetKinds.FirstOrDefault();
        SelectedTarget ??= Targets.FirstOrDefault();
        SelectedLink = selectedLinkId is null ? null : linkedTargets.FirstOrDefault(item => item.Id == selectedLinkId);

        UpdateLinkedTargets(linkedTargets);
        if (SelectedEvidence is null)
        {
            ClearEditor();
        }

        OnPropertyChanged(nameof(IsEmpty));
    }

    public void UpdateLinkedTargets(IReadOnlyList<EvidenceLinkSummaryViewModel> linkedTargets) =>
        ReplaceCollection(LinkedTargets, linkedTargets);

    public void UpdateTargets(IReadOnlyList<TargetOptionViewModel> targets)
    {
        ReplaceCollection(Targets, targets);
        SelectedTarget = Targets.FirstOrDefault();
    }

    private void PopulateEditor(EvidenceSummaryViewModel? evidence)
    {
        if (evidence is null)
        {
            ClearEditor();
            return;
        }

        EvidenceTitle = evidence.Title;
        EvidenceCitation = evidence.Citation ?? string.Empty;
        EvidenceNotes = evidence.Notes ?? string.Empty;
        EvidenceConfidenceText = evidence.Confidence?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private void ClearEditor()
    {
        EvidenceTitle = string.Empty;
        EvidenceCitation = string.Empty;
        EvidenceNotes = string.Empty;
        EvidenceConfidenceText = string.Empty;
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
