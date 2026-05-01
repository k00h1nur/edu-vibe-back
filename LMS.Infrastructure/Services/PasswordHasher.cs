using LMS.Application.Common.Abstractions;

namespace LMS.Infrastructure.Services;

public sealed class PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        return PasswordHashing.Hash(password);
    }

    public bool Verify(string password, string hash)
    {
        return PasswordHashing.Verify(password, hash);
    }
}