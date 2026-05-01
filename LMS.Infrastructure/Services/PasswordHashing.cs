using System.Security.Cryptography;
using System.Text;

namespace LMS.Infrastructure.Services;

public static class PasswordHashing
{
    public static string Hash(string password)
    {
        var salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        return HashWithSalt(password, salt);
    }

    public static string HashWithSalt(string password, string salt)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{salt}:{password}"));
        return $"v1${salt}${Convert.ToBase64String(bytes)}";
    }

    public static bool Verify(string password, string hash)
    {
        var parts = hash.Split('$');
        if (parts.Length != 3 || parts[0] != "v1") return false;
        var expected = HashWithSalt(password, parts[1]);
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(hash));
    }
}