using System.Windows.Input;
using InfoFlowNavigator.Domain.Claims;
using InfoFlowNavigator.Domain.EvidenceLinks;
using InfoFlowNavigator.Domain.Hypotheses;
using InfoFlowNavigator.UI.ViewModels;

namespace InfoFlowNavigator.Application.Tests.ViewModels;

public sealed class EditorWorkflowViewModelTests
{
    [Fact]
    public void BeginNewEvent_ClearsEditorState()
    {
        var viewModel = new EventsViewModel(new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), _ => { });
        viewModel.SelectedEvent = new EventSummaryViewModel(Guid.NewGuid(), "Meeting", DateTimeOffset.UtcNow, "Notes", 0.7, []);

        viewModel.BeginNewEvent();

        Assert.Null(viewModel.SelectedEvent);
        Assert.Equal(string.Empty, viewModel.EventTitle);
        Assert.Equal("Add Event", viewModel.PrimaryActionLabel);
        Assert.False(viewModel.IsEditingExistingItem);
    }

    [Fact]
    public void BeginNewClaim_ClearsEditorState()
    {
        var viewModel = new ClaimsViewModel(new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), _ => { });
        viewModel.SelectedClaim = new ClaimSummaryViewModel(
            Guid.NewGuid(),
            "Claim text",
            ClaimType.General,
            ClaimStatus.Active,
            0.8,
            "Notes",
            null,
            null,
            null);

        viewModel.BeginNewClaim();

        Assert.Null(viewModel.SelectedClaim);
        Assert.Equal(string.Empty, viewModel.Statement);
        Assert.Equal("Add Claim", viewModel.PrimaryActionLabel);
        Assert.False(viewModel.IsEditingExistingItem);
    }

    [Fact]
    public void BeginNewHypothesis_ClearsEditorState()
    {
        var viewModel = new HypothesesViewModel(new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), _ => { });
        viewModel.SelectedHypothesis = new HypothesisSummaryViewModel(
            Guid.NewGuid(),
            "Hypothesis",
            "Statement",
            HypothesisStatus.Active,
            0.7,
            "Notes");

        viewModel.BeginNewHypothesis();

        Assert.Null(viewModel.SelectedHypothesis);
        Assert.Equal(string.Empty, viewModel.Title);
        Assert.Equal("Add Hypothesis", viewModel.PrimaryActionLabel);
        Assert.False(viewModel.IsEditingExistingItem);
    }

    [Fact]
    public void HypothesisValidation_RequiresTitleAndStatement()
    {
        var viewModel = new HypothesesViewModel(new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), _ => { });

        Assert.False(viewModel.CanSaveHypothesis);
        Assert.Equal("Hypothesis title and statement are required.", viewModel.HypothesisValidationMessage);

        viewModel.Title = "Attendance";
        Assert.False(viewModel.CanSaveHypothesis);
        Assert.Equal("Hypothesis statement is required.", viewModel.HypothesisValidationMessage);

        viewModel.Statement = "Alice attended the meeting.";
        Assert.True(viewModel.CanSaveHypothesis);
        Assert.False(viewModel.ShowHypothesisValidationMessage);
    }

    [Fact]
    public void BeginNewEvidence_ClearsEditorState()
    {
        var viewModel = new EvidenceViewModel(new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), _ => { }, _ => { });
        viewModel.Refresh(
            [new EvidenceSummaryViewModel(Guid.NewGuid(), "Evidence", "CIT-1", "Notes", 0.9)],
            null,
            [],
            [new EvidenceLinkTargetKindOptionViewModel(EvidenceLinkTargetKind.Entity, "Entity")],
            [new TargetOptionViewModel(Guid.NewGuid(), "Alice (Person)")],
            null);
        viewModel.SelectedEvidence = viewModel.EvidenceItems[0];

        viewModel.BeginNewEvidence();

        Assert.Null(viewModel.SelectedEvidence);
        Assert.Null(viewModel.SelectedLink);
        Assert.Equal(string.Empty, viewModel.EvidenceTitle);
        Assert.Equal("Add Evidence", viewModel.PrimaryActionLabel);
        Assert.False(viewModel.IsEditingExistingItem);
    }

    [Fact]
    public void PrimaryActionLabel_FlipsBetweenAddAndSave()
    {
        var viewModel = new EventsViewModel(new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), _ => { });

        Assert.Equal("Add Event", viewModel.PrimaryActionLabel);

        viewModel.SelectedEvent = new EventSummaryViewModel(Guid.NewGuid(), "Meeting", DateTimeOffset.UtcNow, null, null, []);

        Assert.Equal("Save Event", viewModel.PrimaryActionLabel);
    }

    [Fact]
    public void Evidence_BeginNewAfterSelection_LeavesAddModeActive()
    {
        var entityId = Guid.NewGuid();
        var evidenceId = Guid.NewGuid();
        var viewModel = new EvidenceViewModel(new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), _ => { }, _ => { });
        viewModel.Refresh(
            [new EvidenceSummaryViewModel(evidenceId, "Evidence", "CIT-1", "Notes", 0.9)],
            evidenceId,
            [new EvidenceLinkSummaryViewModel(
                Guid.NewGuid(),
                evidenceId,
                "Evidence",
                EvidenceLinkTargetKind.Entity,
                entityId,
                "Alice (Person)",
                EvidenceRelationToTarget.Supports,
                EvidenceStrength.Moderate,
                "Linked",
                0.8)],
            [new EvidenceLinkTargetKindOptionViewModel(EvidenceLinkTargetKind.Entity, "Entity")],
            [new TargetOptionViewModel(entityId, "Alice (Person)")],
            null);

        Assert.True(viewModel.IsEditingExistingItem);

        viewModel.BeginNewEvidence();

        Assert.False(viewModel.IsEditingExistingItem);
        Assert.Equal("Add Evidence", viewModel.PrimaryActionLabel);
        Assert.Null(viewModel.SelectedEvidence);
    }

    [Fact]
    public void Evidence_BeginNewAssessment_ClearsSelectedAssessment()
    {
        var entityId = Guid.NewGuid();
        var evidenceId = Guid.NewGuid();
        var linkId = Guid.NewGuid();
        var viewModel = new EvidenceViewModel(new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), _ => { }, _ => { });
        viewModel.Refresh(
            [new EvidenceSummaryViewModel(evidenceId, "Evidence", "CIT-1", "Notes", 0.9)],
            evidenceId,
            [new EvidenceLinkSummaryViewModel(
                linkId,
                evidenceId,
                "Evidence",
                EvidenceLinkTargetKind.Entity,
                entityId,
                "Alice (Person)",
                EvidenceRelationToTarget.Supports,
                EvidenceStrength.Moderate,
                "Linked",
                0.8)],
            [new EvidenceLinkTargetKindOptionViewModel(EvidenceLinkTargetKind.Entity, "Entity")],
            [new TargetOptionViewModel(entityId, "Alice (Person)")],
            linkId);

        Assert.Equal("Update Assessment", viewModel.PrimaryLinkActionLabel);

        viewModel.BeginNewAssessment();

        Assert.Null(viewModel.SelectedLink);
        Assert.Equal("Add Assessment", viewModel.PrimaryLinkActionLabel);
    }

    [Theory]
    [InlineData("21/04/26")]
    [InlineData("21/04/26 14:30")]
    [InlineData("21/04/26 14:30:45")]
    public void ParseOccurredAt_AcceptsAnalystFormat(string text)
    {
        var parsed = EventOccurredAtFormatting.ParseRequired(text);

        Assert.NotEqual(default, parsed);
    }

    [Theory]
    [InlineData("2026-04-21")]
    [InlineData("04/21/26")]
    [InlineData("April 21 2026")]
    [InlineData("not-a-date")]
    [InlineData("")]
    public void ParseOccurredAt_RejectsNonAnalystFormat(string text)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => EventOccurredAtFormatting.ParseRequired(text));

        Assert.Equal(EventOccurredAtFormatting.ValidationMessage, ex.Message);
    }

    [Theory]
    [InlineData("21/04/26", "21/04/26")]
    [InlineData("21/04/26 14:30", "21/04/26 14:30")]
    [InlineData("21/04/26 14:30:45", "21/04/26 14:30:45")]
    public void EventOccurredAt_RoundTripsInAnalystFacingFormat(string text, string expected)
    {
        var parsed = EventOccurredAtFormatting.ParseRequired(text);
        var formatted = EventOccurredAtFormatting.Format(parsed);

        Assert.Equal(expected, formatted);
    }

    [Fact]
    public void EventsViewModel_PopulatesOccurredAtTextInAnalystFacingFormat()
    {
        var parsed = EventOccurredAtFormatting.ParseRequired("21/04/26 14:30");
        var viewModel = new EventsViewModel(new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), _ => { });

        viewModel.SelectedEvent = new EventSummaryViewModel(Guid.NewGuid(), "Meeting", parsed, "Notes", 0.7, []);

        Assert.Equal("21/04/26 14:30", viewModel.EventOccurredAtText);
    }

    [Fact]
    public void ParticipantPrimaryActionLabel_SwitchesBetweenAddAndSave()
    {
        var entityId = Guid.NewGuid();
        var participantId = Guid.NewGuid();
        var viewModel = new EventsViewModel(new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), _ => { });
        viewModel.Refresh([new EventSummaryViewModel(Guid.NewGuid(), "Meeting", DateTimeOffset.UtcNow, null, null, [])], null);
        viewModel.SelectedEvent = viewModel.Events[0];
        viewModel.UpdateParticipants(
            [new EventParticipantSummaryViewModel(participantId, entityId, "Alice (Person)", "attendee", 0.8, null)],
            [new EntityOptionViewModel(entityId, "Alice (Person)")]);

        Assert.Equal("Add Participant", viewModel.ParticipantPrimaryActionLabel);
        Assert.False(viewModel.CanRemoveParticipant);

        viewModel.SelectedParticipant = viewModel.Participants[0];

        Assert.True(viewModel.IsEditingParticipant);
        Assert.Equal("Save Participant", viewModel.ParticipantPrimaryActionLabel);
        Assert.True(viewModel.CanRemoveParticipant);
    }

    [Fact]
    public void ParticipantValidation_RequiresEventEntityAndRole()
    {
        var eventId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var viewModel = new EventsViewModel(new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), _ => { });

        Assert.False(viewModel.CanSaveParticipant);
        Assert.Equal("Select an event first.", viewModel.ParticipantValidationMessage);

        viewModel.Refresh([new EventSummaryViewModel(eventId, "Meeting", DateTimeOffset.UtcNow, null, null, [])], eventId);
        viewModel.UpdateParticipants([], [new EntityOptionViewModel(entityId, "Alice (Person)")]);
        Assert.False(viewModel.CanSaveParticipant);
        Assert.Equal("Select a participant.", viewModel.ParticipantValidationMessage);

        viewModel.SelectedParticipantEntity = viewModel.ParticipantEntities[0];
        Assert.False(viewModel.CanSaveParticipant);
        Assert.Equal("Participant role is required.", viewModel.ParticipantValidationMessage);

        viewModel.ParticipantRole = "attendee";
        Assert.True(viewModel.CanSaveParticipant);
        Assert.False(viewModel.ShowParticipantValidationMessage);
    }

    [Fact]
    public void MainWindow_ShowsVisibleNewButtonsForEventsClaimsAndHypotheses()
    {
        var xamlPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "InfoFlowNavigator.UI", "Views", "MainWindow.axaml");
        var xaml = File.ReadAllText(Path.GetFullPath(xamlPath));

        Assert.Contains("Content=\"New Event\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"New Claim\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"New Hypothesis\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"Add Event\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"Add Claim\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"Add Hypothesis\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void EventSummaryViewModel_GroupsLinkedAttendeesInsideEventCard()
    {
        var summary = new EventSummaryViewModel(
            Guid.NewGuid(),
            "Meeting",
            DateTimeOffset.UtcNow,
            "Notes",
            0.8,
            [
                new EventParticipantRoleGroupViewModel("attendee", ["Alice (Person)", "Bob (Person)"]),
                new EventParticipantRoleGroupViewModel("organizer", ["Carol (Person)"])
            ]);

        Assert.True(summary.HasParticipants);
        Assert.Equal("3 linked participant(s)", summary.ParticipantSummary);
        Assert.Equal("Alice (Person), Bob (Person)", summary.ParticipantRoleGroups[0].AttendeeList);
    }

    private sealed class NoOpCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) { }
    }
}
