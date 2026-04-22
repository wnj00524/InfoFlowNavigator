using InfoFlowNavigator.Application.Abstractions;
using InfoFlowNavigator.Application.Analysis;
using InfoFlowNavigator.Application.Reporting;
using InfoFlowNavigator.Application.Workspaces;
using InfoFlowNavigator.Domain.Claims;
using InfoFlowNavigator.Domain.EvidenceLinks;
using InfoFlowNavigator.Domain.Hypotheses;
using InfoFlowNavigator.Domain.Workspaces;
using InfoFlowNavigator.UI.ViewModels;

namespace InfoFlowNavigator.Application.Tests.Workspaces;

public sealed class WorkspaceApplicationServiceTests
{
    [Fact]
    public void CreateWorkspace_ReturnsNewWorkspaceWithRequestedName()
    {
        var service = new WorkspaceApplicationService(new InMemoryWorkspaceRepository());

        var workspace = service.CreateWorkspace("Bootstrap Workspace");

        Assert.Equal("Bootstrap Workspace", workspace.Name);
    }

    [Fact]
    public void AddUpdateAndRemoveHypothesis_ThroughApplicationService_UpdatesWorkspace()
    {
        var service = new WorkspaceApplicationService(new InMemoryWorkspaceRepository());
        var workspace = service.CreateWorkspace("Bootstrap Workspace");

        workspace = service.AddHypothesis(workspace, "Employment", "Alice works for Contoso.");
        workspace = service.UpdateHypothesis(workspace, workspace.Hypotheses[0].Id, "Employment revised", "Alice likely works for Contoso.", HypothesisStatus.Active, 0.75, "Needs corroboration");
        workspace = service.RemoveHypothesis(workspace, workspace.Hypotheses[0].Id);

        Assert.Empty(workspace.Hypotheses);
    }

    [Fact]
    public void AddAssessmentAndQuerySupportAndContradiction_Work()
    {
        var service = new WorkspaceApplicationService(new InMemoryWorkspaceRepository());
        var workspace = service.CreateWorkspace("Bootstrap Workspace");
        workspace = service.AddHypothesis(workspace, "Employment", "Alice works for Contoso.");
        workspace = service.AddEvidence(workspace, "Interview Summary", "INT-001", "Notes", 0.8);
        workspace = service.AddEvidence(workspace, "Payroll Record", "PAY-007", "Contradictory record", 0.9);

        workspace = service.AddHypothesisEvidenceLink(
            workspace,
            workspace.Evidence[0].Id,
            workspace.Hypotheses[0].Id,
            EvidenceRelationToTarget.Supports,
            EvidenceStrength.Strong,
            "Direct corroboration",
            0.8);

        workspace = service.AddHypothesisEvidenceLink(
            workspace,
            workspace.Evidence[1].Id,
            workspace.Hypotheses[0].Id,
            EvidenceRelationToTarget.Contradicts,
            EvidenceStrength.Moderate,
            "Record mismatch",
            0.7);

        var support = service.GetSupportingEvidenceByTarget(workspace, EvidenceLinkTargetKind.Hypothesis, workspace.Hypotheses[0].Id);
        var contradiction = service.GetContradictingEvidenceByTarget(workspace, EvidenceLinkTargetKind.Hypothesis, workspace.Hypotheses[0].Id);
        var summary = service.GetHypothesisEvidenceSummary(workspace, workspace.Hypotheses[0].Id);

        Assert.Single(support);
        Assert.Single(contradiction);
        Assert.Single(summary.SupportingEvidence);
        Assert.Single(summary.ContradictingEvidence);
    }

    [Fact]
    public void AddClaimAndParticipants_ThroughApplicationService_ExposeQueries()
    {
        var service = new WorkspaceApplicationService(new InMemoryWorkspaceRepository());
        var workspace = service.CreateWorkspace("Bootstrap Workspace");
        workspace = service.AddEntity(workspace, "Alice", "Person");
        workspace = service.AddEvent(workspace, "Meeting");
        workspace = service.AddHypothesis(workspace, "Attendance", "Alice attended the meeting.", HypothesisStatus.Active);

        workspace = service.AddClaim(
            workspace,
            "Alice attended the meeting.",
            ClaimType.EventParticipation,
            ClaimStatus.Active,
            0.8,
            "Working assertion",
            ClaimTargetKind.Event,
            workspace.Events[0].Id,
            workspace.Hypotheses[0].Id);

        workspace = service.AddEventParticipant(workspace, workspace.Events[0].Id, workspace.Entities[0].Id, "attendee", 0.7, "Present in interview notes");

        var claimsByTarget = service.GetClaimsByTarget(workspace, ClaimTargetKind.Event, workspace.Events[0].Id);
        var claimsByHypothesis = service.GetClaimsByHypothesis(workspace, workspace.Hypotheses[0].Id);
        var participants = service.GetParticipantsForEvent(workspace, workspace.Events[0].Id);
        var eventsForEntity = service.GetEventsForEntity(workspace, workspace.Entities[0].Id);

        Assert.Single(claimsByTarget);
        Assert.Single(claimsByHypothesis);
        Assert.Single(participants);
        Assert.Single(eventsForEntity);
    }

    [Fact]
    public void UpdateRelationship_ThroughApplicationService_UpdatesWorkspace()
    {
        var service = new WorkspaceApplicationService(new InMemoryWorkspaceRepository());
        var workspace = service.CreateWorkspace("Bootstrap Workspace");
        workspace = service.AddEntity(workspace, "Alice", "Person");
        workspace = service.AddEntity(workspace, "Contoso", "Organization");
        workspace = service.AddEntity(workspace, "Fabrikam", "Organization");
        workspace = service.AddRelationship(workspace, workspace.Entities[0].Id, workspace.Entities[1].Id, "works_for", "Initial", 0.4);

        workspace = service.UpdateRelationship(
            workspace,
            workspace.Relationships[0].Id,
            workspace.Entities[0].Id,
            workspace.Entities[2].Id,
            "consults_for",
            "Updated",
            0.9);

        Assert.Equal(workspace.Entities[2].Id, workspace.Relationships[0].TargetEntityId);
        Assert.Equal("consults_for", workspace.Relationships[0].RelationshipType);
    }

    [Fact]
    public void UpdateEvidenceAssessment_ThroughApplicationService_UpdatesWorkspace()
    {
        var service = new WorkspaceApplicationService(new InMemoryWorkspaceRepository());
        var workspace = service.CreateWorkspace("Bootstrap Workspace");
        workspace = service.AddEntity(workspace, "Alice", "Person");
        workspace = service.AddEvent(workspace, "Meeting");
        workspace = service.AddEvidence(workspace, "Interview Summary");
        workspace = service.AddEvidenceAssessment(
            workspace,
            workspace.Evidence[0].Id,
            EvidenceLinkTargetKind.Entity,
            workspace.Entities[0].Id,
            EvidenceRelationToTarget.Supports,
            EvidenceStrength.Moderate,
            "Initial",
            0.4);

        workspace = service.UpdateEvidenceAssessment(
            workspace,
            workspace.EvidenceLinks[0].Id,
            workspace.Evidence[0].Id,
            EvidenceLinkTargetKind.Event,
            workspace.Events[0].Id,
            EvidenceRelationToTarget.Contradicts,
            EvidenceStrength.Strong,
            "Updated",
            0.9);

        Assert.Equal(EvidenceLinkTargetKind.Event, workspace.EvidenceLinks[0].TargetKind);
        Assert.Equal(workspace.Events[0].Id, workspace.EvidenceLinks[0].TargetId);
        Assert.Equal(EvidenceRelationToTarget.Contradicts, workspace.EvidenceLinks[0].RelationToTarget);
    }

    [Fact]
    public async Task OpenWorkspaceCommand_UsesFileDialogSelection()
    {
        var repository = new TrackingWorkspaceRepository
        {
            WorkspaceToLoad = AnalysisWorkspace.CreateNew("Loaded Workspace")
        };
        var fileDialog = new FakeWorkspaceFileDialogService
        {
            OpenPath = @"C:\cases\loaded.ifn.json"
        };
        var viewModel = CreateShellViewModel(repository, fileDialog);

        viewModel.OpenWorkspaceCommand.Execute(null);
        await WaitForConditionAsync(() => repository.LoadedPath is not null);

        Assert.Equal(fileDialog.OpenPath, repository.LoadedPath);
        Assert.Equal(fileDialog.OpenPath, viewModel.WorkspacePath);
        Assert.Equal("Loaded Workspace", viewModel.WorkspaceName);
    }

    [Fact]
    public async Task SaveWorkspaceCommand_WithEmptyPath_UsesSaveDialogPath()
    {
        var repository = new TrackingWorkspaceRepository();
        var fileDialog = new FakeWorkspaceFileDialogService
        {
            SavePath = @"C:\cases\saved.ifn.json"
        };
        var viewModel = CreateShellViewModel(repository, fileDialog);
        viewModel.WorkspaceName = "Case Save";

        viewModel.SaveWorkspaceCommand.Execute(null);
        await WaitForConditionAsync(() => repository.SavedPath is not null);

        Assert.Equal(fileDialog.SavePath, repository.SavedPath);
        Assert.Equal(fileDialog.SavePath, viewModel.WorkspacePath);
        Assert.Equal("Case Save", repository.SavedWorkspace?.Name);
        Assert.Equal(1, fileDialog.SaveCallCount);
    }

    [Fact]
    public async Task SaveWorkspaceCommand_WithExistingPath_SavesDirectly()
    {
        var repository = new TrackingWorkspaceRepository();
        var fileDialog = new FakeWorkspaceFileDialogService();
        var viewModel = CreateShellViewModel(repository, fileDialog);
        viewModel.WorkspacePath = @"C:\cases\existing.ifn.json";
        viewModel.WorkspaceName = "Existing Case";

        viewModel.SaveWorkspaceCommand.Execute(null);
        await WaitForConditionAsync(() => repository.SavedPath is not null);

        Assert.Equal(@"C:\cases\existing.ifn.json", repository.SavedPath);
        Assert.Equal(0, fileDialog.SaveCallCount);
    }

    [Fact]
    public void SaveEvent_WithNoSelection_AddsAndSelectsCreatedEvent()
    {
        var viewModel = CreateShellViewModel(new TrackingWorkspaceRepository(), new FakeWorkspaceFileDialogService());
        viewModel.Events.EventTitle = "Meeting";
        viewModel.Events.EventNotes = "Observed";
        viewModel.Events.EventOccurredAtText = "21/04/26 14:30";

        viewModel.Events.SaveEventCommand.Execute(null);

        Assert.Single(viewModel.Workspace.Events);
        Assert.NotNull(viewModel.Events.SelectedEvent);
        Assert.Equal("Meeting", viewModel.Events.SelectedEvent!.Title);
        Assert.Equal("Save Event", viewModel.Events.PrimaryActionLabel);
    }

    [Fact]
    public void BeginNewCommands_OpenCorrectEditorModeAndSection()
    {
        var viewModel = CreateShellViewModel(new TrackingWorkspaceRepository(), new FakeWorkspaceFileDialogService());

        viewModel.Events.BeginNewEventCommand.Execute(null);
        Assert.True(viewModel.IsSpotlightComposerOpen);
        Assert.Equal(SpotlightComposerMode.Event, viewModel.SpotlightMode);
        Assert.Equal(WorkbenchSection.Events, viewModel.SelectedSection?.Section);

        viewModel.Claims.BeginNewClaimCommand.Execute(null);
        Assert.Equal(SpotlightComposerMode.Claim, viewModel.SpotlightMode);
        Assert.Equal(WorkbenchSection.Claims, viewModel.SelectedSection?.Section);

        viewModel.Hypotheses.BeginNewHypothesisCommand.Execute(null);
        Assert.Equal(SpotlightComposerMode.Hypothesis, viewModel.SpotlightMode);
        Assert.Equal(WorkbenchSection.Hypotheses, viewModel.SelectedSection?.Section);

        viewModel.Evidence.BeginNewEvidenceCommand.Execute(null);
        Assert.Equal(SpotlightComposerMode.Evidence, viewModel.SpotlightMode);
        Assert.Equal(WorkbenchSection.Evidence, viewModel.SelectedSection?.Section);
    }

    [Fact]
    public void SaveClaim_WithNoSelection_AddsAndSelectsCreatedClaim()
    {
        var viewModel = CreateShellViewModel(new TrackingWorkspaceRepository(), new FakeWorkspaceFileDialogService());
        viewModel.Claims.Statement = "Alice attended the meeting.";

        viewModel.Claims.SaveClaimCommand.Execute(null);

        Assert.Single(viewModel.Workspace.Claims);
        Assert.NotNull(viewModel.Claims.SelectedClaim);
        Assert.Equal("Save Claim", viewModel.Claims.PrimaryActionLabel);
    }

    [Fact]
    public void AddEntity_SelectsCreatedEntityAndClearsQuickAddForm()
    {
        var viewModel = CreateShellViewModel(new TrackingWorkspaceRepository(), new FakeWorkspaceFileDialogService());
        viewModel.ShowEntitiesCommand.Execute(null);
        viewModel.Entities.NewEntityName = "Alice";
        viewModel.Entities.NewEntityType = "Person";

        viewModel.Entities.AddEntityCommand.Execute(null);

        Assert.Equal(WorkbenchSection.Entities, viewModel.SelectedSection?.Section);
        Assert.NotNull(viewModel.Entities.SelectedEntity);
        Assert.Equal("Alice", viewModel.Entities.SelectedEntity!.Name);
        Assert.Equal("Alice", viewModel.Entities.EditorName);
        Assert.Equal(string.Empty, viewModel.Entities.NewEntityName);
        Assert.Equal("Person", viewModel.Entities.NewEntityType);
    }

    [Fact]
    public void SaveHypothesis_WithNoSelection_AddsAndSelectsCreatedHypothesis()
    {
        var viewModel = CreateShellViewModel(new TrackingWorkspaceRepository(), new FakeWorkspaceFileDialogService());
        viewModel.Hypotheses.Title = "Attendance";
        viewModel.Hypotheses.Statement = "Alice attended the meeting.";

        viewModel.Hypotheses.SaveHypothesisCommand.Execute(null);

        Assert.Single(viewModel.Workspace.Hypotheses);
        Assert.NotNull(viewModel.Hypotheses.SelectedHypothesis);
        Assert.Equal("Save Hypothesis", viewModel.Hypotheses.PrimaryActionLabel);
    }

    [Fact]
    public async Task SuccessStatus_AutoClearsAfterTimeout()
    {
        var viewModel = CreateShellViewModel(
            new TrackingWorkspaceRepository(),
            new FakeWorkspaceFileDialogService(),
            TimeSpan.FromMilliseconds(50));
        viewModel.Entities.NewEntityName = "Alice";
        viewModel.Entities.NewEntityType = "Person";

        viewModel.Entities.AddEntityCommand.Execute(null);

        Assert.Equal("Added entity to the workspace.", viewModel.StatusMessage);
        await WaitForConditionAsync(() => string.IsNullOrEmpty(viewModel.StatusMessage));
        Assert.Equal(string.Empty, viewModel.StatusMessage);
    }

    [Fact]
    public void SaveHypothesis_WithEmptyTitle_IsBlocked()
    {
        var viewModel = CreateShellViewModel(new TrackingWorkspaceRepository(), new FakeWorkspaceFileDialogService());
        viewModel.Hypotheses.Statement = "Alice attended the meeting.";

        viewModel.Hypotheses.SaveHypothesisCommand.Execute(null);

        Assert.Empty(viewModel.Workspace.Hypotheses);
        Assert.Equal("Hypothesis title and statement are required.", viewModel.StatusMessage);
    }

    [Fact]
    public void SaveHypothesis_WithEmptyStatement_IsBlocked()
    {
        var viewModel = CreateShellViewModel(new TrackingWorkspaceRepository(), new FakeWorkspaceFileDialogService());
        viewModel.Hypotheses.Title = "Attendance";

        viewModel.Hypotheses.SaveHypothesisCommand.Execute(null);

        Assert.Empty(viewModel.Workspace.Hypotheses);
        Assert.Equal("Hypothesis title and statement are required.", viewModel.StatusMessage);
    }

    [Fact]
    public void SaveEvidence_WithNoSelection_AddsAndSelectsCreatedEvidence()
    {
        var viewModel = CreateShellViewModel(new TrackingWorkspaceRepository(), new FakeWorkspaceFileDialogService());
        viewModel.Evidence.EvidenceTitle = "Interview Summary";

        viewModel.Evidence.SaveEvidenceCommand.Execute(null);

        Assert.Single(viewModel.Workspace.Evidence);
        Assert.NotNull(viewModel.Evidence.SelectedEvidence);
        Assert.Equal("Save Evidence", viewModel.Evidence.PrimaryActionLabel);
    }

    [Fact]
    public void SelectingEntity_PopulatesEntityEditorFields()
    {
        var viewModel = CreateShellViewModel(new TrackingWorkspaceRepository(), new FakeWorkspaceFileDialogService());
        viewModel.Entities.NewEntityName = "Alice";
        viewModel.Entities.NewEntityType = "Person";
        viewModel.Entities.AddEntityCommand.Execute(null);

        var selected = viewModel.Entities.Entities[0];
        viewModel.Entities.SelectedEntity = selected;

        Assert.Equal("Alice", viewModel.Entities.EditorName);
        Assert.Equal("Person", viewModel.Entities.EditorType);
    }

    [Fact]
    public void SelectingEvent_PopulatesEventEditorFields()
    {
        var viewModel = CreateShellViewModel(new TrackingWorkspaceRepository(), new FakeWorkspaceFileDialogService());
        viewModel.Events.EventTitle = "Meeting";
        viewModel.Events.EventNotes = "Observed";
        viewModel.Events.EventOccurredAtText = "21/04/26 14:30";
        viewModel.Events.SaveEventCommand.Execute(null);

        var selected = viewModel.Events.Events[0];
        viewModel.Events.SelectedEvent = selected;

        Assert.Equal("Meeting", viewModel.Events.EventTitle);
        Assert.Equal("Observed", viewModel.Events.EventNotes);
        Assert.Equal("21/04/26 14:30", viewModel.Events.EventOccurredAtText);
    }

    [Fact]
    public void SelectingClaim_PopulatesClaimEditorFields()
    {
        var viewModel = CreateShellViewModel(new TrackingWorkspaceRepository(), new FakeWorkspaceFileDialogService());
        viewModel.Claims.Statement = "Alice attended.";
        viewModel.Claims.Notes = "Draft";
        viewModel.Claims.SaveClaimCommand.Execute(null);

        var selected = viewModel.Claims.Claims[0];
        viewModel.Claims.SelectedClaim = selected;

        Assert.Equal("Alice attended.", viewModel.Claims.Statement);
        Assert.Equal("Draft", viewModel.Claims.Notes);
    }

    [Fact]
    public void SelectingHypothesis_PopulatesHypothesisEditorFields()
    {
        var viewModel = CreateShellViewModel(new TrackingWorkspaceRepository(), new FakeWorkspaceFileDialogService());
        viewModel.Hypotheses.Title = "Attendance";
        viewModel.Hypotheses.Statement = "Alice attended.";
        viewModel.Hypotheses.Notes = "Working";
        viewModel.Hypotheses.SaveHypothesisCommand.Execute(null);

        var selected = viewModel.Hypotheses.Hypotheses[0];
        viewModel.Hypotheses.SelectedHypothesis = selected;

        Assert.Equal("Attendance", viewModel.Hypotheses.Title);
        Assert.Equal("Alice attended.", viewModel.Hypotheses.Statement);
        Assert.Equal("Working", viewModel.Hypotheses.Notes);
    }

    [Fact]
    public void SelectingEvidence_PopulatesEvidenceEditorFields()
    {
        var viewModel = CreateShellViewModel(new TrackingWorkspaceRepository(), new FakeWorkspaceFileDialogService());
        viewModel.Evidence.EvidenceTitle = "Interview Summary";
        viewModel.Evidence.EvidenceCitation = "INT-001";
        viewModel.Evidence.EvidenceNotes = "Notes";
        viewModel.Evidence.SaveEvidenceCommand.Execute(null);

        var selected = viewModel.Evidence.EvidenceItems[0];
        viewModel.Evidence.SelectedEvidence = selected;

        Assert.Equal("Interview Summary", viewModel.Evidence.EvidenceTitle);
        Assert.Equal("INT-001", viewModel.Evidence.EvidenceCitation);
        Assert.Equal("Notes", viewModel.Evidence.EvidenceNotes);
    }

    [Fact]
    public void InsightPulse_RefreshesAfterMutations()
    {
        var viewModel = CreateShellViewModel(new TrackingWorkspaceRepository(), new FakeWorkspaceFileDialogService());

        Assert.Contains(viewModel.InsightPulseItems, item => item.Title == "Start With Entities");

        viewModel.Claims.BeginNewClaimCommand.Execute(null);
        viewModel.Claims.Statement = "A working claim.";
        viewModel.Claims.SaveClaimCommand.Execute(null);

        Assert.Contains(viewModel.InsightPulseItems, item => item.Title == "Build Chronology");
        Assert.Contains(viewModel.InsightPulseItems, item => item.Title == "Promote Claims Into Hypotheses");
    }

    [Fact]
    public void ToggleRightDrawerCommand_FlipsDrawerState()
    {
        var viewModel = CreateShellViewModel(new TrackingWorkspaceRepository(), new FakeWorkspaceFileDialogService());

        Assert.True(viewModel.IsRightDrawerOpen);

        viewModel.ToggleRightDrawerCommand.Execute(null);
        Assert.False(viewModel.IsRightDrawerOpen);

        viewModel.ToggleRightDrawerCommand.Execute(null);
        Assert.True(viewModel.IsRightDrawerOpen);
    }

    [Fact]
    public void SectionNavigationCommands_SwitchModes()
    {
        var viewModel = CreateShellViewModel(new TrackingWorkspaceRepository(), new FakeWorkspaceFileDialogService());

        viewModel.ShowEventsCommand.Execute(null);
        Assert.True(viewModel.IsEventsMode);

        viewModel.ShowClaimsCommand.Execute(null);
        Assert.True(viewModel.IsClaimsMode);

        viewModel.ShowEvidenceCommand.Execute(null);
        Assert.True(viewModel.IsEvidenceMode);
    }

    [Fact]
    public void BeginNewEvent_ThenSave_AddsNewEventInsteadOfUpdatingSelectedEvent()
    {
        var viewModel = CreateShellViewModel(new TrackingWorkspaceRepository(), new FakeWorkspaceFileDialogService());
        viewModel.Events.EventTitle = "Existing Event";
        viewModel.Events.EventOccurredAtText = "21/04/26";
        viewModel.Events.SaveEventCommand.Execute(null);

        viewModel.Events.BeginNewEventCommand.Execute(null);
        viewModel.Events.EventTitle = "New Event";
        viewModel.Events.EventOccurredAtText = "22/04/26 09:15";
        viewModel.Events.SaveEventCommand.Execute(null);

        Assert.Equal(2, viewModel.Workspace.Events.Count);
        Assert.Contains(viewModel.Workspace.Events, item => item.Title == "Existing Event");
        Assert.Contains(viewModel.Workspace.Events, item => item.Title == "New Event");
    }

    [Fact]
    public void BeginNewClaim_ThenSave_AddsNewClaimInsteadOfUpdatingSelectedClaim()
    {
        var viewModel = CreateShellViewModel(new TrackingWorkspaceRepository(), new FakeWorkspaceFileDialogService());
        viewModel.Claims.Statement = "Existing claim";
        viewModel.Claims.SaveClaimCommand.Execute(null);

        viewModel.Claims.BeginNewClaimCommand.Execute(null);
        viewModel.Claims.Statement = "New claim";
        viewModel.Claims.SaveClaimCommand.Execute(null);

        Assert.Equal(2, viewModel.Workspace.Claims.Count);
        Assert.Contains(viewModel.Workspace.Claims, item => item.Statement == "Existing claim");
        Assert.Contains(viewModel.Workspace.Claims, item => item.Statement == "New claim");
    }

    [Fact]
    public void BeginNewHypothesis_ThenSave_AddsNewHypothesisInsteadOfUpdatingSelectedHypothesis()
    {
        var viewModel = CreateShellViewModel(new TrackingWorkspaceRepository(), new FakeWorkspaceFileDialogService());
        viewModel.Hypotheses.Title = "Existing hypothesis";
        viewModel.Hypotheses.Statement = "Existing statement";
        viewModel.Hypotheses.SaveHypothesisCommand.Execute(null);

        viewModel.Hypotheses.BeginNewHypothesisCommand.Execute(null);
        viewModel.Hypotheses.Title = "New hypothesis";
        viewModel.Hypotheses.Statement = "New statement";
        viewModel.Hypotheses.SaveHypothesisCommand.Execute(null);

        Assert.Equal(2, viewModel.Workspace.Hypotheses.Count);
        Assert.Contains(viewModel.Workspace.Hypotheses, item => item.Title == "Existing hypothesis");
        Assert.Contains(viewModel.Workspace.Hypotheses, item => item.Title == "New hypothesis");
    }

    [Fact]
    public void BeginNewEvidence_ThenSave_AddsNewEvidenceInsteadOfUpdatingSelectedEvidence()
    {
        var viewModel = CreateShellViewModel(new TrackingWorkspaceRepository(), new FakeWorkspaceFileDialogService());
        viewModel.Evidence.EvidenceTitle = "Existing evidence";
        viewModel.Evidence.SaveEvidenceCommand.Execute(null);

        viewModel.Evidence.BeginNewEvidenceCommand.Execute(null);
        viewModel.Evidence.EvidenceTitle = "New evidence";
        viewModel.Evidence.SaveEvidenceCommand.Execute(null);

        Assert.Equal(2, viewModel.Workspace.Evidence.Count);
        Assert.Contains(viewModel.Workspace.Evidence, item => item.Title == "Existing evidence");
        Assert.Contains(viewModel.Workspace.Evidence, item => item.Title == "New evidence");
    }

    [Fact]
    public void BeginNewEvidenceAssessment_ThenSave_AddsNewAssessmentInsteadOfUpdatingSelectedAssessment()
    {
        var viewModel = CreateShellViewModel(new TrackingWorkspaceRepository(), new FakeWorkspaceFileDialogService());
        viewModel.Entities.NewEntityName = "Alice";
        viewModel.Entities.NewEntityType = "Person";
        viewModel.Entities.AddEntityCommand.Execute(null);
        viewModel.Events.EventTitle = "Meeting";
        viewModel.Events.EventOccurredAtText = "21/04/26 14:30";
        viewModel.Events.SaveEventCommand.Execute(null);
        viewModel.Evidence.EvidenceTitle = "Interview Summary";
        viewModel.Evidence.SaveEvidenceCommand.Execute(null);

        viewModel.Evidence.SelectedTargetKind = viewModel.Evidence.TargetKinds.First(item => item.Kind == EvidenceLinkTargetKind.Entity);
        viewModel.Evidence.SelectedTarget = viewModel.Evidence.Targets.First(item => item.Id == viewModel.Workspace.Entities[0].Id);
        viewModel.Evidence.SaveLinkCommand.Execute(null);

        Assert.Single(viewModel.Workspace.EvidenceLinks);
        viewModel.Evidence.SelectedLink = viewModel.Evidence.LinkedTargets[0];

        viewModel.Evidence.BeginNewAssessmentCommand.Execute(null);
        viewModel.Evidence.SelectedTargetKind = viewModel.Evidence.TargetKinds.First(item => item.Kind == EvidenceLinkTargetKind.Event);
        viewModel.Evidence.SelectedTarget = viewModel.Evidence.Targets.First(item => item.Id == viewModel.Workspace.Events[0].Id);
        viewModel.Evidence.SaveLinkCommand.Execute(null);

        Assert.Equal(2, viewModel.Workspace.EvidenceLinks.Count);
        Assert.Contains(viewModel.Workspace.EvidenceLinks, link => link.TargetKind == EvidenceLinkTargetKind.Entity);
        Assert.Contains(viewModel.Workspace.EvidenceLinks, link => link.TargetKind == EvidenceLinkTargetKind.Event);
    }

    [Fact]
    public void EvidenceAssessment_CanTargetRelationship()
    {
        var viewModel = CreateShellViewModel(new TrackingWorkspaceRepository(), new FakeWorkspaceFileDialogService());

        viewModel.Entities.NewEntityName = "Alice";
        viewModel.Entities.NewEntityType = "Person";
        viewModel.Entities.AddEntityCommand.Execute(null);
        viewModel.Entities.NewEntityName = "Contoso";
        viewModel.Entities.NewEntityType = "Organization";
        viewModel.Entities.AddEntityCommand.Execute(null);

        viewModel.Relationships.SelectedSource = viewModel.Relationships.EntityOptions.First(item => item.DisplayName.Contains("Alice", StringComparison.Ordinal));
        viewModel.Relationships.SelectedTarget = viewModel.Relationships.EntityOptions.First(item => item.DisplayName.Contains("Contoso", StringComparison.Ordinal));
        viewModel.Relationships.RelationshipType = "works_for";
        viewModel.Relationships.SaveRelationshipCommand.Execute(null);

        viewModel.Evidence.EvidenceTitle = "Employment record";
        viewModel.Evidence.SaveEvidenceCommand.Execute(null);

        viewModel.Evidence.SelectedTargetKind = viewModel.Evidence.TargetKinds.First(item => item.Kind == EvidenceLinkTargetKind.Relationship);
        viewModel.Evidence.SelectedTarget = viewModel.Evidence.Targets.First();
        viewModel.Evidence.SaveLinkCommand.Execute(null);

        Assert.Single(viewModel.Workspace.EvidenceLinks);
        Assert.Equal(EvidenceLinkTargetKind.Relationship, viewModel.Workspace.EvidenceLinks[0].TargetKind);
    }

    [Fact]
    public void SaveEvent_WithInvalidOccurredAt_SetsClearValidationMessage()
    {
        var viewModel = CreateShellViewModel(new TrackingWorkspaceRepository(), new FakeWorkspaceFileDialogService());
        viewModel.Events.EventTitle = "Meeting";
        viewModel.Events.EventOccurredAtText = "2026-04-21";

        viewModel.Events.SaveEventCommand.Execute(null);

        Assert.Equal(EventOccurredAtFormatting.ValidationMessage, viewModel.StatusMessage);
        Assert.Empty(viewModel.Workspace.Events);
    }

    [Fact]
    public void AddEventParticipant_WithNoSelectedEntity_IsBlocked()
    {
        var viewModel = CreateShellViewModel(new TrackingWorkspaceRepository(), new FakeWorkspaceFileDialogService());
        viewModel.Events.EventTitle = "Meeting";
        viewModel.Events.EventOccurredAtText = "21/04/26 14:30";
        viewModel.Events.SaveEventCommand.Execute(null);

        viewModel.Events.ParticipantRole = "attendee";
        viewModel.Events.AddParticipantCommand.Execute(null);

        Assert.Empty(viewModel.Workspace.EventParticipants);
        Assert.Equal("Select a participant.", viewModel.StatusMessage);
    }

    [Fact]
    public void AddEventParticipant_WithEmptyRole_IsBlocked()
    {
        var viewModel = CreateShellViewModel(new TrackingWorkspaceRepository(), new FakeWorkspaceFileDialogService());
        viewModel.Entities.NewEntityName = "Alice";
        viewModel.Entities.NewEntityType = "Person";
        viewModel.Entities.AddEntityCommand.Execute(null);
        viewModel.Events.EventTitle = "Meeting";
        viewModel.Events.EventOccurredAtText = "21/04/26 14:30";
        viewModel.Events.SaveEventCommand.Execute(null);
        viewModel.Events.SelectedParticipantEntity = viewModel.Events.ParticipantEntities[0];

        viewModel.Events.AddParticipantCommand.Execute(null);

        Assert.Empty(viewModel.Workspace.EventParticipants);
        Assert.Equal("Participant role is required.", viewModel.StatusMessage);
    }

    [Fact]
    public void AddEventParticipant_WithValidInputs_Succeeds()
    {
        var viewModel = CreateShellViewModel(new TrackingWorkspaceRepository(), new FakeWorkspaceFileDialogService());
        viewModel.Entities.NewEntityName = "Alice";
        viewModel.Entities.NewEntityType = "Person";
        viewModel.Entities.AddEntityCommand.Execute(null);
        viewModel.Events.EventTitle = "Meeting";
        viewModel.Events.EventOccurredAtText = "21/04/26 14:30";
        viewModel.Events.SaveEventCommand.Execute(null);
        viewModel.Events.SelectedParticipantEntity = viewModel.Events.ParticipantEntities[0];
        viewModel.Events.ParticipantRole = "attendee";

        viewModel.Events.AddParticipantCommand.Execute(null);

        Assert.Single(viewModel.Workspace.EventParticipants);
        Assert.Equal("Added event participant.", viewModel.StatusMessage);
        Assert.Single(viewModel.Events.Participants);
    }

    private static WorkspaceShellViewModel CreateShellViewModel(
        TrackingWorkspaceRepository repository,
        FakeWorkspaceFileDialogService fileDialogService,
        TimeSpan? transientStatusDuration = null) =>
        new(
            new WorkspaceApplicationService(repository),
            new StubAnalysisService(),
            new StubReportGenerator(),
            new StubWorkspaceExportService(),
            fileDialogService,
            transientStatusDuration);

    private static async Task WaitForConditionAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(20);
        }

        throw new TimeoutException("Timed out waiting for async command completion.");
    }

    private sealed class InMemoryWorkspaceRepository : IWorkspaceRepository
    {
        public Task<AnalysisWorkspace> LoadAsync(string path, CancellationToken cancellationToken = default) =>
            Task.FromResult(AnalysisWorkspace.CreateNew(path));

        public Task SaveAsync(string path, AnalysisWorkspace workspace, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class TrackingWorkspaceRepository : IWorkspaceRepository
    {
        public AnalysisWorkspace WorkspaceToLoad { get; set; } = AnalysisWorkspace.CreateNew("Loaded");

        public string? LoadedPath { get; private set; }

        public string? SavedPath { get; private set; }

        public AnalysisWorkspace? SavedWorkspace { get; private set; }

        public Task<AnalysisWorkspace> LoadAsync(string path, CancellationToken cancellationToken = default)
        {
            LoadedPath = path;
            return Task.FromResult(WorkspaceToLoad);
        }

        public Task SaveAsync(string path, AnalysisWorkspace workspace, CancellationToken cancellationToken = default)
        {
            SavedPath = path;
            SavedWorkspace = workspace;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeWorkspaceFileDialogService : IWorkspaceFileDialogService
    {
        public string? OpenPath { get; init; }

        public string? SavePath { get; init; }

        public int SaveCallCount { get; private set; }

        public Task<string?> PickOpenWorkspacePathAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(OpenPath);

        public Task<string?> PickSaveWorkspacePathAsync(string suggestedFileName, CancellationToken cancellationToken = default)
        {
            SaveCallCount++;
            return Task.FromResult(SavePath);
        }
    }

    private sealed class StubAnalysisService : IAnalysisService
    {
        public Task<WorkspaceAnalysisResult> SummarizeAsync(AnalysisWorkspace workspace, CancellationToken cancellationToken = default) =>
            Task.FromResult(WorkspaceAnalysisResultFactory.Empty(
                workspace.Entities.Count,
                workspace.Relationships.Count,
                workspace.Events.Count,
                workspace.EventParticipants.Count,
                workspace.Claims.Count,
                workspace.Hypotheses.Count,
                workspace.Evidence.Count,
                workspace.EvidenceLinks.Count,
                []));
    }

    private sealed class StubReportGenerator : IReportGenerator
    {
        public Task<ReportArtifact> GenerateAsync(AnalysisWorkspace workspace, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ReportArtifact("briefing.txt", "text/plain", "Briefing"));
    }

    private sealed class StubWorkspaceExportService : IWorkspaceExportService
    {
        public Task ExportAsync(AnalysisWorkspace workspace, string path, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
