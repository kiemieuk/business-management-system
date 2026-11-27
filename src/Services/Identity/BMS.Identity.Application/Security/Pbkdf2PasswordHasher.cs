using System.Globalization;
using System.Security.Cryptography;
using BMS.Identity.Application.Abstractions;

namespace BMS.Identity.Application.Security;

/// <summary>
/// PBKDF2 (HMAC-SHA512) password hasher with a random 128-bit salt per password.
/// Hash format: <c>PBKDF2-SHA512$iterations$base64(salt)$base64(hash)</c>, so the work factor can be raised later
/// without invalidating existing hashes.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    /// <summary>OWASP recommended iteration count for PBKDF2-HMAC-SHA512.</summary>
    public const int DefaultIterations = 210_000;

    private const string Algorithm = "PBKDF2-SHA512";
    private const int SaltSize = 16;
    private const int HashSize = 64;
    private const char Separator = '$';

    private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA512;

    private readonly int _iterations;

    public Pbkdf2PasswordHasher()
        : this(DefaultIterations)
    {
    }

    public Pbkdf2PasswordHasher(int iterations)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(iterations, 1);
        _iterations = iterations;
    }

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, _iterations, HashAlgorithm, HashSize);

        return string.Join(
            Separator,
            Algorithm,
            _iterations.ToString(CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(passwordHash))
        {
            return false;
        }

        var parts = passwordHash.Split(Separator);
        if (parts.Length != 4 || parts[0] != Algorithm ||
            !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var iterations) || iterations < 1)
        {
            return false;
        }

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithm, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
