using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Ahk.GradeManagement
{
    internal static class HeaderDictionaryExtensions
    {
        public static string GetValueOrDefault(this IHeaderDictionary headers, string name)
        {
            if (headers.TryGetValue(name, out var values))
                return values.FirstOrDefault();
            return null;
        }
    }
}
