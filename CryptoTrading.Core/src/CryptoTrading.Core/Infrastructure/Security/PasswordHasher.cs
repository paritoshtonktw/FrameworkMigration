using System;
using System.Security.Cryptography;

namespace CryptoTrading.Infrastructure.Security;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string storedHash);
}

public class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltByteSize = 16;
    private const int HashByteSize = 32;
    private const int Iterations = 10000;

    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentNullException(nameof(password));

        byte[] salt = new byte[SaltByteSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA1, HashByteSize);
        return $"{Iterations}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
            return false;

        var parts = storedHash.Split(':');
        if (parts.Length != 3)
            return false;

        if (!int.TryParse(parts[0], out int iterations))
            return false;

        byte[] salt;
        byte[] expectedHash;
        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expectedHash = Convert.FromBase64String(parts[2]);
        }
        catch
        {
            return false;
        }

        byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA1, expectedHash.Length);
        return SlowEquals(expectedHash, actualHash);
    }

    private static bool SlowEquals(byte[] a, byte[] b)
    {
        uint diff = (uint)a.Length ^ (uint)b.Length;
        for (int i = 0; i < a.Length && i < b.Length; i++)
        {
            diff |= (uint)(a[i] ^ b[i]);
        }
        return diff == 0;
    }
}
