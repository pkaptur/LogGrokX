namespace LogGrokX.Data;

public sealed record LogFormatValidationError(string PropertyName, string Value, string Message);
