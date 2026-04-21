using System.Globalization;

namespace InfoFlowNavigator.UI.ViewModels;

public static class EventOccurredAtFormatting
{
    private static readonly string[] AcceptedFormats =
    [
        "dd/MM/yy",
        "dd/MM/yy HH:mm",
        "dd/MM/yy HH:mm:ss"
    ];

    public const string ValidationMessage = "Occurred At must use DD/MM/YY with an optional time, for example 21/04/26 or 21/04/26 14:30.";

    public static DateTimeOffset ParseRequired(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException(ValidationMessage);
        }

        var trimmed = text.Trim();
        if (!DateTime.TryParseExact(trimmed, AcceptedFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var localDateTime))
        {
            throw new InvalidOperationException(ValidationMessage);
        }

        var localTimeZone = TimeZoneInfo.Local;
        var offset = localTimeZone.GetUtcOffset(localDateTime);
        return new DateTimeOffset(localDateTime, offset).ToUniversalTime();
    }

    public static string Format(DateTimeOffset? occurredAtUtc)
    {
        if (occurredAtUtc is null)
        {
            return string.Empty;
        }

        var localDateTime = TimeZoneInfo.ConvertTime(occurredAtUtc.Value, TimeZoneInfo.Local).DateTime;
        if (localDateTime.TimeOfDay == TimeSpan.Zero)
        {
            return localDateTime.ToString("dd/MM/yy", CultureInfo.InvariantCulture);
        }

        if (localDateTime.Second == 0)
        {
            return localDateTime.ToString("dd/MM/yy HH:mm", CultureInfo.InvariantCulture);
        }

        return localDateTime.ToString("dd/MM/yy HH:mm:ss", CultureInfo.InvariantCulture);
    }
}
