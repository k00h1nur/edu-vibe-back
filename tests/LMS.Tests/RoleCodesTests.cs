using FluentAssertions;
using LMS.Application.Common.Security;
using Xunit;

namespace LMS.Tests;

/// <summary>
/// Locks the set of structural roles the RBAC admin API must protect from
/// rename/delete. If someone drops a code from <see cref="RoleCodes.BuiltIn"/>,
/// the SuperAdmin UI would suddenly allow deleting e.g. the Admin role and brick
/// authorization — this test is the tripwire.
/// </summary>
public sealed class RoleCodesTests
{
    [Theory]
    [InlineData(RoleCodes.Admin)]
    [InlineData(RoleCodes.Teacher)]
    [InlineData(RoleCodes.Student)]
    [InlineData(RoleCodes.SuperAdmin)]
    [InlineData(RoleCodes.AcademyDirector)]
    [InlineData(RoleCodes.OfficeAdmin)]
    [InlineData(RoleCodes.SupportTeacher)]
    public void All_seeded_roles_are_protected_as_builtin(string code)
    {
        RoleCodes.IsBuiltIn(code).Should().BeTrue();
    }

    [Theory]
    [InlineData("admin")]   // case-insensitive
    [InlineData("SUPERADMIN")]
    public void IsBuiltIn_is_case_insensitive(string code)
    {
        RoleCodes.IsBuiltIn(code).Should().BeTrue();
    }

    [Theory]
    [InlineData("CustomRole")]
    [InlineData("Marketing")]
    [InlineData("")]
    [InlineData(null)]
    public void Custom_or_empty_codes_are_not_builtin(string? code)
    {
        RoleCodes.IsBuiltIn(code).Should().BeFalse();
    }
}
