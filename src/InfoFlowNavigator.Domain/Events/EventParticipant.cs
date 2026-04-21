using InfoFlowNavigator.Domain.Common;

namespace InfoFlowNavigator.Domain.Events;

public sealed record EventParticipant(
    Guid Id,
    Guid EventId,
    Guid EntityId,
    string Role,
    double? Confidence,
    string? Notes,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    public static EventParticipant Create(
        Guid eventId,
        Guid entityId,
        string role,
        double? confidence = null,
        string? notes = null)
    {
        ValidateReferences(eventId, entityId);

        var now = DateTimeOffset.UtcNow;
        return new EventParticipant(
            Guid.NewGuid(),
            eventId,
            entityId,
            DomainValidation.Required(role, nameof(role), "Participant role is required."),
            DomainValidation.NormalizeConfidence(confidence, nameof(confidence)),
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            now,
            now);
    }

    public EventParticipant Update(
        string role,
        double? confidence = null,
        string? notes = null)
    {
        ValidateReferences(EventId, EntityId);

        return this with
        {
            Role = DomainValidation.Required(role, nameof(role), "Participant role is required."),
            Confidence = DomainValidation.NormalizeConfidence(confidence, nameof(confidence)),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private static void ValidateReferences(Guid eventId, Guid entityId)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Event id is required.", nameof(eventId));
        }

        if (entityId == Guid.Empty)
        {
            throw new ArgumentException("Entity id is required.", nameof(entityId));
        }
    }
}
