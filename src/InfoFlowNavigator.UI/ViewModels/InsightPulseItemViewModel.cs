namespace InfoFlowNavigator.UI.ViewModels;

public sealed record InsightPulseItemViewModel(
    string Title,
    string Detail,
    string Severity,
    WorkbenchSection TargetSection,
    bool IsActionable);
