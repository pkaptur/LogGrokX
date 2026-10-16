using System;
using System.Collections.Generic;
using System.Linq;
using LogGrokX.Data;
using LogGrokX.Localization;

namespace LogGrokX.Settings;

internal static class LogFormatValidation
{
    public static IEnumerable<string> GetMessages(LogFormat format) => format.GetValidationErrors()
        .Select(error => error.PropertyName == nameof(LogFormat.IndexedFields)
            ? Strings.Format("IndexedFieldUnknown", error.Value, string.Join(", ", format.FieldNames.DefaultIfEmpty("—")))
            : Strings.Get("LogRegexInvalid"))
        .Distinct();

    public static string WithContext(string profileName, int formatNumber, string message) =>
        Strings.Format("LogFormatError", profileName, formatNumber, message);

    public static void ValidateProfile(ProfileSettings profile)
    {
        var errors = (profile.LogFormats ?? [])
            .SelectMany((format, index) => GetMessages(format)
                .Select(message => WithContext(profile.Name, index + 1, message)))
            .ToArray();
        if (errors.Length > 0)
            throw new FormatException(string.Join(Environment.NewLine, errors));
    }
}
