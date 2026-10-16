using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace LogGrokX.Data
{
    public class LogFormat
    {
        public string Regex { get; set; } = string.Empty;

        public string[] FieldNames => GetFieldNames(Regex); 

        public string[] IndexedFields { get; set; } = Array.Empty<string>();

        public string TimeField { get; set; } = string.Empty;

        public string TimeFormat { get; set; } = string.Empty;

        public string[] Transformations { get; set; } = Array.Empty<string>();

        public byte XorMask { get; set; } = 0;

        public bool IsCorrect()
        {
            var errors = GetValidationErrors();
            foreach (var error in errors)
                Trace.TraceError($"Invalid log format ({error.PropertyName}): {error.Message}");
            return errors.Count == 0;
        }

        public IReadOnlyList<LogFormatValidationError> GetValidationErrors()
        {
            Regex regex;
            try
            {
                regex = new Regex(Regex);
            }
            catch (ArgumentException e)
            {
                return [new(nameof(Regex), Regex, e.Message)];
            }

            var errors = new List<LogFormatValidationError>();
            foreach (var transformation in Transformations)
            {
                try
                {
                    _ = new Regex(transformation);
                }
                catch (ArgumentException e)
                {
                    errors.Add(new(nameof(Transformations), transformation, e.Message));
                }
            }
            errors.AddRange(GetIndexedFieldErrors(regex));
            return errors;
        }

        public int[] IndexedFieldNumbers 
        {
            get
            {
                var regex = new Regex(Regex);
                var error = GetIndexedFieldErrors(regex).FirstOrDefault();
                if (error != null)
                    throw new ArgumentException(error.Message, nameof(IndexedFields));
                return IndexedFields.Select(n => regex.GroupNumberFromName(n) - 1).ToArray();
            }
        }

        private IEnumerable<LogFormatValidationError> GetIndexedFieldErrors(Regex regex)
        {
            foreach (var field in IndexedFields)
            {
                if (regex.GroupNumberFromName(field) <= 0)
                    yield return new(nameof(IndexedFields), field,
                        $"Indexed field '{field}' does not refer to a capture group in the log format regex.");
            }
        }

        private string[] GetFieldNames(string regexString)
        {
            if (_fieldNames != null) return _fieldNames;
            var groupNames = new Regex(regexString).GetGroupNames();
            _fieldNames = groupNames[1..].ToArray();

            return _fieldNames;
        }

        private string[]? _fieldNames;
    }
}
