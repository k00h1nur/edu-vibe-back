namespace LMS.Application.Common.Abstractions;

public interface IJwtTokenGenerator
{
    string Generate(Guid userId, string email, IEnumerable<string> roles, IEnumerable<string> permissions);
}