using System.Security.Cryptography;

namespace EVotingSystem.Services;

public class PasswordHasher
{
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public (string Hash, string Salt) HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public bool Verify(string password, string hash, string salt)
    {
        var decodedSalt = Convert.FromBase64String(salt);
        var computedHash = Rfc2898DeriveBytes.Pbkdf2(password, decodedSalt, Iterations, HashAlgorithmName.SHA256, HashSize);
        var decodedHash = Convert.FromBase64String(hash);
        return CryptographicOperations.FixedTimeEquals(computedHash, decodedHash);
    }
}
