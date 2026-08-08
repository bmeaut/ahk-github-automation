using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Ahk.Web.Services;

/// <summary>
/// Compares two signature strings without leaking, through how long the comparison took, how much of a guess
/// was correct.
///
/// <para>Both webhook schemes in this application originally compared their signatures with
/// <c>string.Equals</c>, which returns at the first differing character. That is the textbook setup for a
/// remote timing attack: an attacker who can measure response times refines a forged signature one character
/// at a time, turning an infeasible search into a linear one. Neither validator's accept/reject behaviour
/// changes by fixing it — only the timing does.</para>
///
/// <para>The length check is deliberately left fast and early. Signature length is fixed by the algorithm and
/// public knowledge, so it is not a secret to leak.</para>
/// </summary>
internal static class SignatureComparison
{
    public static bool FixedTimeEquals(string received, string expected)
    {
        if (received.Length != expected.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(
            MemoryMarshal.AsBytes(received.AsSpan()),
            MemoryMarshal.AsBytes(expected.AsSpan()));
    }
}
