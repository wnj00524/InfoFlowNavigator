using System.Collections.ObjectModel;
using InfoFlowNavigator.Application.Analysis;

namespace InfoFlowNavigator.UI.ViewModels;

public sealed class FindingsViewModel : ViewModelBase
{
    private string _topPrioritySummary = "Priority findings will appear here.";
    private string _supportCoverageSummary = "Support gaps will appear here.";
    private string _timelineCoverageSummary = "Chronology guidance will appear here.";
    private string _inferenceSummary = "Hypothesis guidance will appear here.";

    public ObservableCollection<AnalysisFinding> Findings { get; } = [];

    public ObservableCollection<AnalysisFinding> TopPriorityFindings { get; } = [];

    public ObservableCollection<FindingGroupViewModel> SeverityGroups { get; } = [];

    public ObservableCollection<string> UnsupportedRelationships { get; } = [];

    public ObservableCollection<string> UnsupportedEvents { get; } = [];

    public ObservableCollection<string> ActivityWithoutEvents { get; } = [];

    public ObservableCollection<string> ChronologyGaps { get; } = [];

    public ObservableCollection<string> HypothesisConflicts { get; } = [];

    public ObservableCollection<string> UnresolvedHypotheses { get; } = [];

    public ObservableCollection<string> CollectionGuidance { get; } = [];

    public ObservableCollection<string> UnsupportedClaims { get; } = [];

    public ObservableCollection<string> EventParticipationGaps { get; } = [];

    public ObservableCollection<string> RepeatedCoOccurrences { get; } = [];

    public string TopPrioritySummary
    {
        get => _topPrioritySummary;
        private set => SetProperty(ref _topPrioritySummary, value);
    }

    public string SupportCoverageSummary
    {
        get => _supportCoverageSummary;
        private set => SetProperty(ref _supportCoverageSummary, value);
    }

    public string TimelineCoverageSummary
    {
        get => _timelineCoverageSummary;
        private set => SetProperty(ref _timelineCoverageSummary, value);
    }

    public string InferenceSummary
    {
        get => _inferenceSummary;
        private set => SetProperty(ref _inferenceSummary, value);
    }

    public void Refresh(WorkspaceAnalysisResult analysis)
    {
        ReplaceCollection(Findings, analysis.Findings);
        ReplaceCollection(TopPriorityFindings, analysis.Findings.Take(5).ToArray());
        ReplaceCollection(UnsupportedRelationships, analysis.RelationshipsWithoutSupportingEvidence.Select(item => item.Description).ToArray());
        ReplaceCollection(UnsupportedEvents, analysis.EventsWithoutSupportingEvidence.Select(item => item.Title).ToArray());
        ReplaceCollection(UnsupportedClaims, analysis.UnsupportedClaims.Select(item => item.Statement).ToArray());
        ReplaceCollection(ActivityWithoutEvents, analysis.EntitiesWithActivityButNoEvents.Select(item => $"{item.Name} ({item.Degree})").ToArray());
        ReplaceCollection(ChronologyGaps, analysis.ChronologyGaps.Select(item => $"{item.EarlierEventTitle} -> {item.LaterEventTitle} ({item.GapDays} days)").ToArray());
        ReplaceCollection(EventParticipationGaps, analysis.EventParticipationGaps.Select(item => $"{item.EventTitle}: {item.Detail}").ToArray());
        ReplaceCollection(RepeatedCoOccurrences, analysis.RepeatedCoOccurrences.Select(item => $"{item.FirstEntityName} + {item.SecondEntityName} ({item.SharedEventCount})").ToArray());
        ReplaceCollection(HypothesisConflicts, analysis.HypothesisConflicts.Select(item => $"{item.Title}: {item.Detail}").ToArray());
        ReplaceCollection(UnresolvedHypotheses, analysis.UnresolvedHypotheses.Select(item => $"{item.Title}: {item.Detail}").ToArray());
        ReplaceCollection(CollectionGuidance, analysis.CollectionGuidance.Select(item => $"{item.Title}: {item.Detail}").ToArray());
        ReplaceCollection(SeverityGroups, analysis.Findings
            .GroupBy(item => item.Severity)
            .OrderByDescending(group => group.Key)
            .Select(group => new FindingGroupViewModel(group.Key.ToString(), group.ToArray()))
            .ToArray());

        TopPrioritySummary = analysis.Findings.Count == 0
            ? "No findings generated yet."
            : $"{analysis.Findings.Count(item => item.Severity == FindingSeverity.Critical)} critical, {analysis.Findings.Count(item => item.Severity == FindingSeverity.Warning)} warning, and {analysis.Findings.Count(item => item.Severity == FindingSeverity.Info)} informational findings.";

        SupportCoverageSummary =
            $"Relationships without support: {analysis.RelationshipsWithoutSupportingEvidence.Count}; " +
            $"events without support: {analysis.EventsWithoutSupportingEvidence.Count}; " +
            $"claims without support: {analysis.UnsupportedClaims.Count}.";

        TimelineCoverageSummary = analysis.ChronologyGaps.Count == 0
            ? "No large chronology gaps detected in dated events."
            : $"{analysis.ChronologyGaps.Count} chronology gaps of 30 days or more need review.";

        InferenceSummary = analysis.HypothesisSummaries.Count == 0
            ? "No hypotheses have been added yet."
            : $"{analysis.HypothesisSummaries.Count} hypotheses tracked; {analysis.UnresolvedHypotheses.Count} unresolved, {analysis.HypothesisConflicts.Count} in conflict, and {analysis.ClaimHypothesisImpacts.Count} active claims directly affecting active hypotheses.";
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
