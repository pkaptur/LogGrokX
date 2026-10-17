using System.Collections.Generic;
using System.Diagnostics;
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

    public static LogFormat[] GetValidFormats(ProfileSettings profile)
    {
        return (profile.LogFormats ?? [])
            .Where((format, index) =>
            {
                var errors = GetMessages(format).ToArray();
                foreach (var error in errors)
                    Trace.TraceWarning($"Skipping invalid log format: {WithContext(profile.Name, index + 1, error)}");
                return errors.Length == 0;
            })
            .ToArray();
    }
}
