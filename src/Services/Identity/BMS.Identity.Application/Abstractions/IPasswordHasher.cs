namespace BMS.Identity.Application.Abstractions;

/// <summary>
/// One-way, salted password hashing. Passwords are never stored or logged in plain text.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);
}
