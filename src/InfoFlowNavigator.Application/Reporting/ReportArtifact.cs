namespace InfoFlowNavigator.Application.Reporting;

public sealed record ReportArtifact(
    string FileName,
    string ContentType,
    string Content);
