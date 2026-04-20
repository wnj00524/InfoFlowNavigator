using System.Collections.ObjectModel;
using InfoFlowNavigator.Application.Analysis;

namespace InfoFlowNavigator.UI.ViewModels;

public sealed class OverviewViewModel : ViewModelBase
{
    private string _workspaceName = "Untitled Workspace";
    private int _entityCount;
    private int _relationshipCount;
    private int _eventCount;
    private int _evidenceCount;
    private int _evidenceLinkCount;
    private string _keyMessage = "Start by adding entities, evidence, and events.";

    public ObservableCollection<AnalysisFinding> TopFindings { get; } = [];

    public string WorkspaceName
    {
        get => _workspaceName;
        private set => SetProperty(ref _workspaceName, value);
    }

    public int EntityCount
    {
        get => _entityCount;
        private set => SetProperty(ref _entityCount, value);
    }

    public int RelationshipCount
    {
        get => _relationshipCount;
        private set => SetProperty(ref _relationshipCount, value);
    }

    public int EventCount
    {
        get => _eventCount;
        private set => SetProperty(ref _eventCount, value);
    }

    public int EvidenceCount
    {
        get => _evidenceCount;
        private set => SetProperty(ref _evidenceCount, value);
    }

    public int EvidenceLinkCount
    {
        get => _evidenceLinkCount;
        private set => SetProperty(ref _evidenceLinkCount, value);
    }

    public string KeyMessage
    {
        get => _keyMessage;
        private set => SetProperty(ref _keyMessage, value);
    }

    public void Refresh(string workspaceName, WorkspaceAnalysisResult analysis)
    {
        WorkspaceName = workspaceName;
        EntityCount = analysis.EntityCount;
        RelationshipCount = analysis.RelationshipCount;
        EventCount = analysis.EventCount;
        EvidenceCount = analysis.EvidenceCount;
        EvidenceLinkCount = analysis.EvidenceLinkCount;

        TopFindings.Clear();
        foreach (var finding in analysis.Findings.Take(4))
        {
            TopFindings.Add(finding);
        }

        KeyMessage = analysis.Findings.FirstOrDefault()?.Detail ?? "No findings generated yet.";
    }
}
