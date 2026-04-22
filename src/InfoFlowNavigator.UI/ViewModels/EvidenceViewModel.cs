using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using InfoFlowNavigator.Domain.EvidenceLinks;

namespace InfoFlowNavigator.UI.ViewModels;

public sealed class EvidenceViewModel : EditorWorkflowViewModel
{
    private readonly Action<EvidenceSummaryViewModel?> _selectionChanged;
    private readonly Action<EvidenceLinkTargetKind> _targetKindChanged;
    private bool _isRefreshingLinkEditor;
    private EvidenceSummaryViewModel? _selectedEvidence;
    private string _evidenceTitle = string.Empty;
    private string _evidenceCitation = string.Empty;
    private string _evidenceNotes = string.Empty;
    private string _evidenceConfidenceText = string.Empty;
    private EvidenceLinkSummaryViewModel? _selectedLink;
    private EvidenceLinkTargetKindOptionViewModel? _selectedTargetKind;
    private TargetOptionViewModel? _selectedTarget;
    private EvidenceRelationOptionViewModel? _selectedRelation;
    private EvidenceStrengthOptionViewModel? _selectedStrength;
    private string _linkNotes = string.Empty;
    private string _linkConfidenceText = string.Empty;

    public EvidenceViewModel(
        ICommand beginNewEvidenceCommand,
        ICommand saveEvidenceCommand,
        ICommand deleteEvidenceCommand,
        ICommand beginNewAssessmentCommand,
        ICommand saveLinkCommand,
        ICommand deleteLinkCommand,
        Action<EvidenceSummaryViewModel?> selectionChanged,
        Action<EvidenceLinkTargetKind> targetKindChanged)
    {
        BeginNewEvidenceCommand = beginNewEvidenceCommand;
        SaveEvidenceCommand = saveEvidenceCommand;
        DeleteEvidenceCommand = deleteEvidenceCommand;
        BeginNewAssessmentCommand = beginNewAssessmentCommand;
        SaveLinkCommand = saveLinkCommand;
        DeleteLinkCommand = deleteLinkCommand;
        _selectionChanged = selectionChanged;
        _targetKindChanged = targetKindChanged;

        foreach (var relation in new[]
                 {
                     new EvidenceRelationOptionViewModel(EvidenceRelationToTarget.Supports, "Supports"),
                     new EvidenceRelationOptionViewModel(EvidenceRelationToTarget.Contradicts, "Contradicts"),
                     new EvidenceRelationOptionViewModel(EvidenceRelationToTarget.Mentions, "Mentions"),
                     new EvidenceRelationOptionViewModel(EvidenceRelationToTarget.Contextual, "Contextual"),
                     new EvidenceRelationOptionViewModel(EvidenceRelationToTarget.DerivedFrom, "Derived From")
                 })
        {
            Relations.Add(relation);
        }

        foreach (var strength in new[]
                 {
                     new EvidenceStrengthOptionViewModel(EvidenceStrength.Weak, "Weak"),
                     new EvidenceStrengthOptionViewModel(EvidenceStrength.Moderate, "Moderate"),
                     new EvidenceStrengthOptionViewModel(EvidenceStrength.Strong, "Strong")
                 })
        {
            Strengths.Add(strength);
        }

        SelectedRelation = Relations.FirstOrDefault();
        SelectedStrength = Strengths.FirstOrDefault(item => item.Strength == EvidenceStrength.Moderate);
    }

    protected override string ItemTypeDisplayName => "Evidence";

    protected override string CreateHintText => "Capture source material first, then attach it where it supports, contradicts, or contextualizes the analysis.";

    protected override string EditHintText => "Update the selected evidence and manage its structured assessments.";

    public ObservableCollection<EvidenceSummaryViewModel> EvidenceItems { get; } = [];

    public ObservableCollection<EvidenceLinkSummaryViewModel> LinkedTargets { get; } = [];

    public ObservableCollection<EvidenceLinkTargetKindOptionViewModel> TargetKinds { get; } = [];

    public ObservableCollection<TargetOptionViewModel> Targets { get; } = [];

    public ObservableCollection<EvidenceRelationOptionViewModel> Relations { get; } = [];

    public ObservableCollection<EvidenceStrengthOptionViewModel> Strengths { get; } = [];

    public ICommand BeginNewEvidenceCommand { get; }

    public ICommand SaveEvidenceCommand { get; }

    public ICommand DeleteEvidenceCommand { get; }

    public ICommand BeginNewAssessmentCommand { get; }

    public ICommand SaveLinkCommand { get; }

    public ICommand DeleteLinkCommand { get; }

    public EvidenceSummaryViewModel? SelectedEvidence
    {
        get => _selectedEvidence;
        set
        {
            if (SetEditorSelection(ref _selectedEvidence, value, _selectionChanged, PopulateEvidenceEditor, ClearEvidenceEditor, nameof(SelectedEvidence)))
            {
                if (value is null)
                {
                    BeginNewAssessment();
                }

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
        set
        {
            if (SetProperty(ref _selectedLink, value))
            {
                if (value is not null && SelectedTargetKind?.Kind != value.TargetKind)
                {
                    SelectedTargetKind = TargetKinds.FirstOrDefault(item => item.Kind == value.TargetKind) ?? TargetKinds.FirstOrDefault();
                }

                PopulateLinkEditor(value);
                OnPropertyChanged(nameof(PrimaryLinkActionLabel));
            }
        }
    }

    public EvidenceLinkTargetKindOptionViewModel? SelectedTargetKind
    {
        get => _selectedTargetKind;
        set
        {
            if (SetProperty(ref _selectedTargetKind, value) && value is not null && !_isRefreshingLinkEditor)
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

    public EvidenceRelationOptionViewModel? SelectedRelation
    {
        get => _selectedRelation;
        set => SetProperty(ref _selectedRelation, value);
    }

    public EvidenceStrengthOptionViewModel? SelectedStrength
    {
        get => _selectedStrength;
        set => SetProperty(ref _selectedStrength, value);
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

    public bool IsEmpty => EvidenceItems.Count == 0;

    public string LinkHint => SelectedEvidence is null
        ? "Select an evidence item to add assessments."
        : $"Create or update a structured assessment for '{SelectedEvidence.Title}'.";

    public string PrimaryLinkActionLabel => SelectedLink is null ? "Add Assessment" : "Update Assessment";

    public bool IsEditingAssessment => SelectedLink is not null;

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
        ReplaceCollection(LinkedTargets, linkedTargets);

        SelectedEvidence = selectedEvidenceId is null ? null : EvidenceItems.FirstOrDefault(item => item.Id == selectedEvidenceId);

        _isRefreshingLinkEditor = true;
        try
        {
            SelectedTargetKind ??= TargetKinds.FirstOrDefault();
            SelectedTarget ??= Targets.FirstOrDefault();
            SelectedLink = selectedLinkId is null ? null : LinkedTargets.FirstOrDefault(item => item.Id == selectedLinkId);
        }
        finally
        {
            _isRefreshingLinkEditor = false;
        }

        if (SelectedEvidence is null)
        {
            ClearEvidenceEditor();
            ClearLinkEditor();
        }
        else if (SelectedLink is not null)
        {
            PopulateLinkEditor(SelectedLink);
        }

        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(PrimaryLinkActionLabel));
    }

    public void UpdateLinkedTargets(IReadOnlyList<EvidenceLinkSummaryViewModel> linkedTargets)
    {
        var selectedLinkId = SelectedLink?.Id;
        ReplaceCollection(LinkedTargets, linkedTargets);
        SelectedLink = selectedLinkId is null ? null : LinkedTargets.FirstOrDefault(item => item.Id == selectedLinkId);
        if (SelectedLink is null)
        {
            ClearLinkEditor();
        }
    }

    public void UpdateTargets(IReadOnlyList<TargetOptionViewModel> targets)
    {
        var selectedTargetId = SelectedTarget?.Id;
        ReplaceCollection(Targets, targets);
        SelectedTarget = selectedTargetId is null
            ? Targets.FirstOrDefault()
            : Targets.FirstOrDefault(item => item.Id == selectedTargetId) ?? Targets.FirstOrDefault();
    }

    public void BeginNewAssessment()
    {
        SelectedLink = null;
        ClearLinkEditor();
    }

    public void BeginNewEvidence()
    {
        EnterAddMode();
        SelectedEvidence = null;
        ClearEvidenceEditor();
        BeginNewAssessment();
    }

    private void PopulateEvidenceEditor(EvidenceSummaryViewModel? evidence)
    {
        if (evidence is null)
        {
            ClearEvidenceEditor();
            return;
        }

        EvidenceTitle = evidence.Title;
        EvidenceCitation = evidence.Citation ?? string.Empty;
        EvidenceNotes = evidence.Notes ?? string.Empty;
        EvidenceConfidenceText = evidence.Confidence?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private void PopulateLinkEditor(EvidenceLinkSummaryViewModel? link)
    {
        _isRefreshingLinkEditor = true;
        try
        {
            if (link is null)
            {
                ClearLinkEditor();
                return;
            }

            SelectedTargetKind = TargetKinds.FirstOrDefault(item => item.Kind == link.TargetKind) ?? TargetKinds.FirstOrDefault();
            SelectedTarget = Targets.FirstOrDefault(item => item.Id == link.TargetId) ?? Targets.FirstOrDefault();
            SelectedRelation = Relations.FirstOrDefault(item => item.Relation == link.RelationToTarget) ?? Relations.FirstOrDefault();
            SelectedStrength = Strengths.FirstOrDefault(item => item.Strength == link.Strength) ?? Strengths.FirstOrDefault();
            LinkNotes = link.Notes ?? string.Empty;
            LinkConfidenceText = link.Confidence?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        }
        finally
        {
            _isRefreshingLinkEditor = false;
        }
    }

    private void ClearEvidenceEditor()
    {
        EvidenceTitle = string.Empty;
        EvidenceCitation = string.Empty;
        EvidenceNotes = string.Empty;
        EvidenceConfidenceText = string.Empty;
    }

    private void ClearLinkEditor()
    {
        _isRefreshingLinkEditor = true;
        try
        {
            LinkNotes = string.Empty;
            LinkConfidenceText = string.Empty;
            SelectedRelation = Relations.FirstOrDefault();
            SelectedStrength = Strengths.FirstOrDefault(item => item.Strength == EvidenceStrength.Moderate);
            SelectedTargetKind ??= TargetKinds.FirstOrDefault();
            SelectedTarget = Targets.FirstOrDefault();
        }
        finally
        {
            _isRefreshingLinkEditor = false;
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
