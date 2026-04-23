using System.Windows.Input;
using InfoFlowNavigator.Domain.Claims;
using InfoFlowNavigator.Domain.Events;
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
        var viewModel = new ClaimsViewModel(new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), _ => { }, _ => { });
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
            [new EventParticipantSummaryViewModel(participantId, entityId, "Alice (Person)", EventEntityLinkCategory.Participant, "Attendee", 0.8, null)],
            [new EntityOptionViewModel(entityId, "Alice (Person)")]);

        Assert.Equal("Add Link", viewModel.ParticipantPrimaryActionLabel);
        Assert.False(viewModel.CanRemoveParticipant);

        viewModel.SelectedParticipant = viewModel.Participants[0];

        Assert.True(viewModel.IsEditingParticipant);
        Assert.Equal("Save Link", viewModel.ParticipantPrimaryActionLabel);
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
        Assert.Equal("Select an entity to link.", viewModel.ParticipantValidationMessage);

        viewModel.SelectedParticipantEntity = viewModel.ParticipantEntities[0];
        Assert.Equal("Participant is ready to save.", viewModel.ParticipantValidationMessage);
        Assert.True(viewModel.CanSaveParticipant);

        viewModel.SelectedParticipantCategory = viewModel.ParticipantCategories.First(item => item.Category == EventEntityLinkCategory.Other);
        Assert.False(viewModel.CanSaveParticipant);
        Assert.Equal("Add a short detail for Other links.", viewModel.ParticipantValidationMessage);

        viewModel.ParticipantRole = "attendee";
        Assert.True(viewModel.CanSaveParticipant);
        Assert.False(viewModel.ShowParticipantValidationMessage);
    }

    [Fact]
    public void MainWindow_UsesTopRailAndHonestNewItemLabels()
    {
        var xamlPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "InfoFlowNavigator.UI", "Views", "MainWindow.axaml");
        var xaml = File.ReadAllText(Path.GetFullPath(xamlPath));

        Assert.Contains("Classes=\"top-rail nav-rail\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Add Entity...\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"New Event...\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"New Claim...\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"New Hypothesis...\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"New Evidence...\"", xaml, StringComparison.Ordinal);
        Assert.Contains("WindowDecorations=\"None\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("ColumnDefinitions=\"2.3*,1.2*,1*\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<WrapPanel />", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"Claim Details\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"Link Entities To Event\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"{Binding InsightPulseButtonLabel}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"Evidence Assessment Editor\"", xaml, StringComparison.Ordinal);
        Assert.Contains("PlaceholderText=\"Search targets by name, type, or statement\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"New Assessment\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<view:SpotlightComposer", xaml, StringComparison.Ordinal);
        Assert.Contains("<view:InsightPulseBar", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<ComboBox", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void ClaimsViewModel_TargetKindChangeRequestsUpdatedTargets()
    {
        ClaimTargetKind? changedKind = null;
        var viewModel = new ClaimsViewModel(new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), _ => { }, kind => changedKind = kind);

        viewModel.SelectedTargetKind = viewModel.TargetKinds.First(item => item.Kind == ClaimTargetKind.Relationship);

        Assert.Equal(ClaimTargetKind.Relationship, changedKind);
    }

    [Fact]
    public void EvidenceViewModel_TargetSearchFiltersSuggestions()
    {
        var personId = Guid.NewGuid();
        var relationshipId = Guid.NewGuid();
        var viewModel = new EvidenceViewModel(new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), _ => { }, _ => { });
        viewModel.Refresh(
            [new EvidenceSummaryViewModel(Guid.NewGuid(), "Memo", "CIT-1", null, 0.8)],
            null,
            [],
            [new EvidenceLinkTargetKindOptionViewModel(EvidenceLinkTargetKind.Relationship, "Relationship")],
            [
                new TargetOptionViewModel(personId, "Alice (Person)"),
                new TargetOptionViewModel(relationshipId, "Alice -> works_for -> Contoso")
            ],
            null);

        viewModel.TargetSearchText = "works_for";

        Assert.Single(viewModel.FilteredTargets);
        Assert.Equal(relationshipId, viewModel.FilteredTargets[0].Id);
    }

    [Fact]
    public void EvidenceViewModel_SelectingTarget_ClearsSuggestions()
    {
        var personId = Guid.NewGuid();
        var relationshipId = Guid.NewGuid();
        var viewModel = new EvidenceViewModel(new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), _ => { }, _ => { });
        viewModel.Refresh(
            [new EvidenceSummaryViewModel(Guid.NewGuid(), "Memo", "CIT-1", null, 0.8)],
            null,
            [],
            [new EvidenceLinkTargetKindOptionViewModel(EvidenceLinkTargetKind.Relationship, "Relationship")],
            [
                new TargetOptionViewModel(personId, "Alice (Person)"),
                new TargetOptionViewModel(relationshipId, "Alice -> works_for -> Contoso")
            ],
            null);

        viewModel.TargetSearchText = "works_for";
        viewModel.SelectedTarget = viewModel.FilteredTargets[0];

        Assert.Equal("Alice -> works_for -> Contoso", viewModel.TargetSearchText);
        Assert.Empty(viewModel.FilteredTargets);
    }

    [Fact]
    public void ClaimsViewModel_SelectingAutocompleteTargetClearsSuggestions()
    {
        var targetId = Guid.NewGuid();
        var viewModel = new ClaimsViewModel(new NoOpCommand(), new NoOpCommand(), new NoOpCommand(), _ => { }, _ => { });
        viewModel.UpdateTargets(
            [
                new TargetOptionViewModel(targetId, "Alice -> works_for -> Contoso")
            ]);

        viewModel.TargetPicker.SearchText = "works_for";
        viewModel.TargetPicker.SelectedItem = viewModel.TargetPicker.Suggestions[0];

        Assert.Equal("Alice -> works_for -> Contoso", viewModel.TargetPicker.SearchText);
        Assert.False(viewModel.TargetPicker.HasSuggestions);
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
                new EventParticipantRoleGroupViewModel(EventEntityLinkCategory.Participant, "Participants", ["Alice (Person)", "Bob (Person)"]),
                new EventParticipantRoleGroupViewModel(EventEntityLinkCategory.Organization, "Organizations", ["Carol (Person)"])
            ]);

        Assert.True(summary.HasParticipants);
        Assert.Equal($"2 participants {Convert.ToChar(0x00B7)} 1 organization", summary.ParticipantSummary);
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
