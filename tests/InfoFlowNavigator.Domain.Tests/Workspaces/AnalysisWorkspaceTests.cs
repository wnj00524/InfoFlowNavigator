using InfoFlowNavigator.Domain.Entities;
using WorkspaceEvidence = InfoFlowNavigator.Domain.Evidence.Evidence;
using InfoFlowNavigator.Domain.Claims;
using InfoFlowNavigator.Domain.EvidenceLinks;
using InfoFlowNavigator.Domain.Events;
using InfoFlowNavigator.Domain.Hypotheses;
using InfoFlowNavigator.Domain.Relationships;
using InfoFlowNavigator.Domain.Workspaces;

namespace InfoFlowNavigator.Domain.Tests.Workspaces;

public sealed class AnalysisWorkspaceTests
{
    [Fact]
    public void CreateNew_InitializesEmptyCollectionsAndSchema()
    {
        var workspace = AnalysisWorkspace.CreateNew("Case Alpha");

        Assert.Equal(AnalysisWorkspace.CurrentSchemaVersion, workspace.SchemaVersion);
        Assert.Equal("Case Alpha", workspace.Name);
        Assert.Empty(workspace.Entities);
        Assert.Empty(workspace.Relationships);
        Assert.Empty(workspace.Events);
        Assert.Empty(workspace.Hypotheses);
        Assert.Empty(workspace.Evidence);
        Assert.Empty(workspace.EvidenceLinks);
    }

    [Fact]
    public void UpdateEntity_UpdatesValuesAndWorkspaceUpdatedAtUtc()
    {
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        var entity = new Entity(Guid.NewGuid(), "Alice", "Person", "Initial notes", 0.2, [], new Dictionary<string, string>(), createdAt, createdAt);
        var workspace = new AnalysisWorkspace(
            AnalysisWorkspace.CurrentSchemaVersion,
            Guid.NewGuid(),
            "Case Alpha",
            null,
            [],
            createdAt,
            createdAt,
            [entity],
            [],
            [],
            [],
            [],
            [],
            [],
            []);

        var updatedEntity = entity.Update("Alice Smith", "Subject", "Updated notes", 0.8);
        var updatedWorkspace = workspace.UpdateEntity(updatedEntity);

        Assert.Equal("Alice Smith", updatedWorkspace.Entities[0].Name);
        Assert.Equal("Subject", updatedWorkspace.Entities[0].EntityType);
        Assert.True(updatedWorkspace.UpdatedAtUtc > workspace.UpdatedAtUtc);
    }

    [Fact]
    public void AddUpdateAndRemoveHypothesis_Succeeds()
    {
        var workspace = AnalysisWorkspace.CreateNew("Case Alpha");
        var hypothesis = Hypothesis.Create("H1", "Alice works for Contoso.");

        var added = workspace.AddHypothesis(hypothesis);
        var updated = added.UpdateHypothesis(hypothesis.Update("H1 revised", "Alice probably works for Contoso.", HypothesisStatus.Active, 0.7, "Working line"));
        var removed = updated.RemoveHypothesis(updated.Hypotheses[0].Id);

        Assert.Single(added.Hypotheses);
        Assert.Equal("H1 revised", updated.Hypotheses[0].Title);
        Assert.Empty(removed.Hypotheses);
    }

    [Fact]
    public void RemoveHypothesis_WithReferencingEvidenceAssessment_Throws()
    {
        var evidence = WorkspaceEvidence.Create("Interview");
        var hypothesis = Hypothesis.Create("Employment", "Alice works for Contoso.");
        var workspace = AnalysisWorkspace.CreateNew("Case Alpha")
            .AddEvidence(evidence)
            .AddHypothesis(hypothesis)
            .AddEvidenceLink(EvidenceLink.Create(evidence.Id, EvidenceLinkTargetKind.Hypothesis, hypothesis.Id, EvidenceRelationToTarget.Supports));

        var act = () => workspace.RemoveHypothesis(hypothesis.Id);

        var ex = Assert.Throws<InvalidOperationException>(act);
        Assert.Contains("evidence assessments still reference", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddEvidenceAssessment_WithMissingEvidence_Throws()
    {
        var hypothesis = Hypothesis.Create("Employment", "Alice works for Contoso.");
        var workspace = AnalysisWorkspace.CreateNew("Case Alpha").AddHypothesis(hypothesis);

        var act = () => workspace.AddEvidenceLink(EvidenceLink.Create(Guid.NewGuid(), EvidenceLinkTargetKind.Hypothesis, hypothesis.Id, EvidenceRelationToTarget.Supports));

        var ex = Assert.Throws<InvalidOperationException>(act);
        Assert.Contains("existing evidence", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddAndRemoveEvidenceAssessment_Succeeds()
    {
        var entity = Entity.Create("Alice", "Person");
        var evidence = WorkspaceEvidence.Create("Interview");
        var workspace = AnalysisWorkspace.CreateNew("Case Alpha")
            .AddEntity(entity)
            .AddEvidence(evidence);

        var withLink = workspace.AddEvidenceLink(EvidenceLink.Create(
            evidence.Id,
            EvidenceLinkTargetKind.Entity,
            entity.Id,
            EvidenceRelationToTarget.Contextual,
            EvidenceStrength.Moderate,
            "Mentions Alice"));
        var removed = withLink.RemoveEvidenceLink(withLink.EvidenceLinks[0].Id);

        Assert.Single(withLink.EvidenceLinks);
        Assert.Equal(EvidenceRelationToTarget.Contextual, withLink.EvidenceLinks[0].RelationToTarget);
        Assert.Empty(removed.EvidenceLinks);
    }

    [Fact]
    public void AddClaimAndParticipant_ValidateReferencesAndDuplicates()
    {
        var entity = Entity.Create("Alice", "Person");
        var @event = Event.Create("Meeting");
        var hypothesis = Hypothesis.Create("H1", "Alice attended the meeting.");
        var workspace = AnalysisWorkspace.CreateNew("Case Alpha")
            .AddEntity(entity)
            .AddEvent(@event)
            .AddHypothesis(hypothesis);

        var claim = Claim.Create(
            "Alice attended the meeting.",
            ClaimType.EventParticipation,
            ClaimStatus.Active,
            0.8,
            targetKind: ClaimTargetKind.Event,
            targetId: @event.Id,
            hypothesisId: hypothesis.Id);

        var withClaim = workspace.AddClaim(claim);
        var withParticipant = withClaim.AddEventParticipant(EventParticipant.Create(@event.Id, entity.Id, "attendee", 0.7));

        Assert.Single(withClaim.Claims);
        Assert.Single(withParticipant.EventParticipants);

        var duplicateAct = () => withParticipant.AddEventParticipant(EventParticipant.Create(@event.Id, entity.Id, "attendee", 0.6));
        var duplicateEx = Assert.Throws<InvalidOperationException>(duplicateAct);
        Assert.Contains("same event, entity, and role", duplicateEx.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RemoveClaim_WithReferencingEvidenceAssessment_Throws()
    {
        var claim = Claim.Create("Alice attended Event A.", ClaimType.EventParticipation, ClaimStatus.Active);
        var evidence = WorkspaceEvidence.Create("Interview");
        var workspace = AnalysisWorkspace.CreateNew("Case Alpha")
            .AddClaim(claim)
            .AddEvidence(evidence)
            .AddEvidenceLink(EvidenceLink.Create(evidence.Id, EvidenceLinkTargetKind.Claim, claim.Id, EvidenceRelationToTarget.Supports));

        var act = () => workspace.RemoveClaim(claim.Id);

        var ex = Assert.Throws<InvalidOperationException>(act);
        Assert.Contains("evidence assessments still reference", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
