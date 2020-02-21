using Microsoft.AspNetCore.Http;
using System.Linq;

namespace Ahk.GitHub.Monitor
{
    internal static class Extensions
    {
        public static string GetValueOrDefault(this IHeaderDictionary headers, string name)
        {
            if (headers.TryGetValue(name, out var values))
                return values.FirstOrDefault();
            return null;
        }
    }
}
