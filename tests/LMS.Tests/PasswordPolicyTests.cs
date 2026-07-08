using FluentAssertions;
using LMS.Application.Common.Security;
using Xunit;

namespace LMS.Tests;

/// <summary>
/// Coverage for <see cref="PasswordPolicy"/> — the shared strength rules behind
/// the self-service change-password flow (mirrored on the frontend by
/// <c>lib/password.ts</c>). Verifies the length bounds and the "3 of 4
/// character classes" requirement.
/// </summary>
public sealed class PasswordPolicyTests
{
    [Theory]
    [InlineData("Str0ng!Pass")]   // upper + lower + digit + symbol
    [InlineData("Abcdef1g")]       // upper + lower + digit (3 classes, 8 chars)
    [InlineData("passWORD123")]    // lower + upper + digit
    public void Accepts_passwords_meeting_length_and_complexity(string password)
    {
        PasswordPolicy.Validate(password).Should().BeNull();
        PasswordPolicy.IsValid(password).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_missing_password(string? password)
    {
        PasswordPolicy.Validate(password).Should().NotBeNull();
    }

    [Fact]
    public void Rejects_too_short()
    {
        PasswordPolicy.Validate("Ab1!x").Should().Contain("at least");
    }

    [Fact]
    public void Rejects_too_long()
    {
        var big = new string('a', PasswordPolicy.MaxLength) + "A1!";
        PasswordPolicy.Validate(big).Should().Contain("at most");
    }

    [Theory]
    [InlineData("alllowercase")]   // 1 class
    [InlineData("password1")]      // lower + digit only = 2 classes
    [InlineData("12345678")]       // 1 class
    public void Rejects_when_fewer_than_three_character_classes(string password)
    {
        PasswordPolicy.Validate(password).Should().Contain("three of");
        PasswordPolicy.IsValid(password).Should().BeFalse();
    }

    [Fact]
    public void Counts_character_classes()
    {
        PasswordPolicy.CharacterClassCount("aB1!").Should().Be(4);
        PasswordPolicy.CharacterClassCount("abc").Should().Be(1);
        PasswordPolicy.CharacterClassCount("Abc").Should().Be(2);
    }
}
