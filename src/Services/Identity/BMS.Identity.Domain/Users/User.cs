using BMS.BuildingBlocks.Domain.Entities;
using BMS.BuildingBlocks.Domain.Exceptions;
using BMS.Identity.Domain.Roles;

namespace BMS.Identity.Domain.Users;

/// <summary>
/// A person who can sign in to the system with a username and password.
/// </summary>
public sealed class User : AuditableEntity, IAggregateRoot
{
    public const int UsernameMaxLength = 64;
    public const int EmailMaxLength = 256;
    public const int FullNameMaxLength = 128;

    private readonly List<UserRole> _userRoles = [];

    private User()
    {
    }

    public string Username { get; private set; } = string.Empty;

    /// <summary>Upper-cased username used for case-insensitive unique lookups.</summary>
    public string NormalizedUsername { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string FullName { get; private set; } = string.Empty;

    /// <summary>Salted, one-way PBKDF2 hash of the password. The plain password is never stored.</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    public bool IsActive { get; private set; } = true;

    public DateTimeOffset? LastLoginAt { get; private set; }

    public int AccessFailedCount { get; private set; }

    public DateTimeOffset? LockoutEnd { get; private set; }

    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    public static User Create(string username, string email, string fullName, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new DomainException("Username is required.");
        }

        var user = new User
        {
            Username = username.Trim(),
            NormalizedUsername = Normalize(username),
            PasswordHash = passwordHash,
        };
        user.UpdateProfile(email, fullName);
        return user;
    }

    public static string Normalize(string username) => username.Trim().ToUpperInvariant();

    public void UpdateProfile(string email, string fullName)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new DomainException("Full name is required.");
        }

        Email = email.Trim();
        FullName = fullName.Trim();
    }

    public void ChangePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Password hash is required.");
        }

        PasswordHash = passwordHash;
        ResetLockout();
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public bool IsLockedOut(DateTimeOffset now) => LockoutEnd.HasValue && LockoutEnd.Value > now;

    public void RecordSuccessfulLogin(DateTimeOffset now)
    {
        LastLoginAt = now;
        ResetLockout();
    }

    /// <summary>
    /// Records a failed sign-in attempt and locks the account once <paramref name="maxFailedAttempts"/> is reached.
    /// </summary>
    public void RecordFailedLogin(DateTimeOffset now, int maxFailedAttempts, TimeSpan lockoutDuration)
    {
        AccessFailedCount++;
        if (maxFailedAttempts > 0 && AccessFailedCount >= maxFailedAttempts)
        {
            LockoutEnd = now.Add(lockoutDuration);
            AccessFailedCount = 0;
        }
    }

    public void ResetLockout()
    {
        AccessFailedCount = 0;
        LockoutEnd = null;
    }

    /// <summary>Replaces the user's roles with the given set.</summary>
    public void SetRoles(IEnumerable<Role> roles)
    {
        var desired = roles.Select(r => r.Id).ToHashSet();
        _userRoles.RemoveAll(ur => !desired.Contains(ur.RoleId));
        foreach (var roleId in desired.Where(id => _userRoles.All(ur => ur.RoleId != id)))
        {
            _userRoles.Add(new UserRole(Id, roleId));
        }
    }
}
