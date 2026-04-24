using InfoFlowNavigator.Domain.Common;

namespace InfoFlowNavigator.Domain.Events;

public sealed record EventParticipant(
    Guid Id,
    Guid EventId,
    Guid EntityId,
    EventEntityLinkCategory Category,
    string? RoleDetail,
    double? Confidence,
    string? Notes,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    public string Role => FormatRole(Category, RoleDetail);

    public static EventParticipant Create(
        Guid eventId,
        Guid entityId,
        EventEntityLinkCategory category,
        string? roleDetail = null,
        double? confidence = null,
        string? notes = null)
    {
        ValidateReferences(eventId, entityId);

        var now = DateTimeOffset.UtcNow;
        return new EventParticipant(
            Guid.NewGuid(),
            eventId,
            entityId,
            category,
            NormalizeRoleDetail(roleDetail),
            DomainValidation.NormalizeConfidence(confidence, nameof(confidence)),
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            now,
            now);
    }

    public static EventParticipant Create(
        Guid eventId,
        Guid entityId,
        string role,
        double? confidence = null,
        string? notes = null)
    {
        var category = InferCategory(role);
        var roleDetail = InferRoleDetail(role, category);
        return Create(eventId, entityId, category, roleDetail, confidence, notes);
    }

    public EventParticipant Update(
        EventEntityLinkCategory category,
        string? roleDetail = null,
        double? confidence = null,
        string? notes = null)
    {
        ValidateReferences(EventId, EntityId);

        return this with
        {
            Category = category,
            RoleDetail = NormalizeRoleDetail(roleDetail),
            Confidence = DomainValidation.NormalizeConfidence(confidence, nameof(confidence)),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public EventParticipant Update(
        string role,
        double? confidence = null,
        string? notes = null)
    {
        var category = InferCategory(role);
        var roleDetail = InferRoleDetail(role, category);
        return Update(category, roleDetail, confidence, notes);
    }

    public static EventEntityLinkCategory InferCategory(string? legacyRole)
    {
        if (string.IsNullOrWhiteSpace(legacyRole))
        {
            return EventEntityLinkCategory.Participant;
        }

        var normalized = legacyRole.Trim().ToLowerInvariant();
        if (normalized.Contains("location") || normalized.Contains("site") || normalized.Contains("address") || normalized.Contains("venue"))
        {
            return EventEntityLinkCategory.Location;
        }

        if (normalized.Contains("device") || normalized.Contains("phone") || normalized.Contains("imei") || normalized.Contains("laptop") || normalized.Contains("handset"))
        {
            return EventEntityLinkCategory.Device;
        }

        if (normalized.Contains("vehicle") || normalized.Contains("car") || normalized.Contains("truck") || normalized.Contains("van") || normalized.Contains("boat") || normalized.Contains("aircraft"))
        {
            return EventEntityLinkCategory.Vehicle;
        }

        if (normalized.Contains("organization") || normalized.Contains("company") || normalized.Contains("agency") || normalized.Contains("department"))
        {
            return EventEntityLinkCategory.Organization;
        }

        if (normalized.Contains("source") || normalized.Contains("witness") || normalized.Contains("informant"))
        {
            return EventEntityLinkCategory.Source;
        }

        return EventEntityLinkCategory.Participant;
    }

    public static string? InferRoleDetail(string? legacyRole, EventEntityLinkCategory category)
    {
        var normalized = NormalizeRoleDetail(legacyRole);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return string.Equals(normalized, GetCategoryDisplayName(category), StringComparison.OrdinalIgnoreCase)
            ? null
            : normalized;
    }

    public static string GetCategoryDisplayName(EventEntityLinkCategory category) =>
        category switch
        {
            EventEntityLinkCategory.Participant => "Participant",
            EventEntityLinkCategory.Location => "Location",
            EventEntityLinkCategory.Device => "Device",
            EventEntityLinkCategory.Vehicle => "Vehicle",
            EventEntityLinkCategory.Organization => "Organization",
            EventEntityLinkCategory.Source => "Source",
            EventEntityLinkCategory.Other => "Other",
            _ => "Participant"
        };

    public static string GetCategoryGroupTitle(EventEntityLinkCategory category) =>
        category switch
        {
            EventEntityLinkCategory.Participant => "Participants",
            EventEntityLinkCategory.Location => "Locations",
            EventEntityLinkCategory.Device => "Devices",
            EventEntityLinkCategory.Vehicle => "Vehicles",
            EventEntityLinkCategory.Organization => "Organizations",
            EventEntityLinkCategory.Source => "Sources",
            EventEntityLinkCategory.Other => "Other Links",
            _ => "Participants"
        };

    private static string? NormalizeRoleDetail(string? roleDetail) =>
        string.IsNullOrWhiteSpace(roleDetail) ? null : roleDetail.Trim();

    private static string FormatRole(EventEntityLinkCategory category, string? roleDetail)
    {
        var categoryName = GetCategoryDisplayName(category);
        return string.IsNullOrWhiteSpace(roleDetail)
            ? categoryName
            : $"{categoryName}: {roleDetail.Trim()}";
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
