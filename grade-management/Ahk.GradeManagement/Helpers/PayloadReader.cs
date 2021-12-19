using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;

namespace Ahk.GradeManagement
{
    internal static class PayloadReader
    {
        public static bool TryGetPayload<T>(string requestBody, [NotNullWhen(true)] out T result, [NotNullWhen(false)] out string error)
        {
            error = null;
            result = default(T);
            try
            {
                result = JsonSerializer.Deserialize<T>(requestBody);

                var validationResults = new List<ValidationResult>();
                var isValid = Validator.TryValidateObject(result, new ValidationContext(result, null, null), validationResults, true);
                if (!isValid)
                {
                    error = $"Body cannot be deserialized as JSON: {string.Join(", ", validationResults.Select(s => s.ErrorMessage).ToArray())}";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                error = $"Body cannot be deserialized as JSON: {ex.Message}";
                return false;
            }
        }
    }
}
