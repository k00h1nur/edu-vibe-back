using System.Linq;

namespace LMS.Application.Common.Security;

/// <summary>
/// Single source of truth for password strength rules, shared by any flow that
/// SETS a password (currently the self-service change-password handler). The
/// frontend mirrors these exact rules in <c>lib/password.ts</c> for live
/// feedback — keep the two in sync.
///
/// Rules: 8–128 chars and at least three of the four character classes
/// (uppercase, lowercase, digit, symbol). Deliberately not a dictionary/breach
/// check — that belongs behind a service call, not an inline validator.
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const int MaxLength = 128;
    public const int RequiredCharClasses = 3;

    /// <summary>Number of distinct character classes present (0–4).</summary>
    public static int CharacterClassCount(string password)
    {
        var classes = 0;
        if (password.Any(char.IsUpper)) classes++;
        if (password.Any(char.IsLower)) classes++;
        if (password.Any(char.IsDigit)) classes++;
        if (password.Any(c => !char.IsLetterOrDigit(c))) classes++;
        return classes;
    }

    /// <summary>
    /// Returns a human-readable reason the password is unacceptable, or
    /// <c>null</c> when it satisfies the policy.
    /// </summary>
    public static string? Validate(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return "Password is required.";
        if (password.Length < MinLength)
            return $"Password must be at least {MinLength} characters.";
        if (password.Length > MaxLength)
            return $"Password must be at most {MaxLength} characters.";
        if (CharacterClassCount(password) < RequiredCharClasses)
            return "Password must include at least three of: an uppercase letter, a lowercase letter, a number, and a symbol.";
        return null;
    }

    /// <summary>True when the password satisfies the policy.</summary>
    public static bool IsValid(string? password) => Validate(password) is null;
}
