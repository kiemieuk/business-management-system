using BMS.BuildingBlocks.Domain.Abstractions;
using BMS.BuildingBlocks.Domain.Exceptions;
using BMS.Identity.Application.Abstractions;
using BMS.Identity.Application.Contracts;
using BMS.Identity.Application.Security;
using BMS.Identity.Domain.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BMS.Identity.Application.Services;

public interface IAuthService
{
    /// <summary>Validates the credentials and issues an access token, or returns <c>null</c> when sign-in fails.</summary>
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
}

public sealed class AuthService(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    TimeProvider timeProvider,
    IOptions<AccountSecurityOptions> securityOptions,
    ILogger<AuthService> logger) : IAuthService
{
    private static readonly Lazy<string> DummyHash = new(() => new Pbkdf2PasswordHasher().Hash(Guid.NewGuid().ToString()));

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var user = await users.GetByUsernameAsync(request.Username, cancellationToken);

        if (user is null)
        {
            // Spend the same time as a real verification so that response timing does not reveal valid usernames.
            passwordHasher.Verify(request.Password, DummyHash.Value);
            logger.LogInformation("Failed sign-in: unknown user");
            return null;
        }

        if (!user.IsActive || user.IsLockedOut(now))
        {
            logger.LogWarning("Failed sign-in for user {UserId}: account inactive or locked out", user.Id);
            return null;
        }

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            var options = securityOptions.Value;
            user.RecordFailedLogin(now, options.MaxFailedAccessAttempts, TimeSpan.FromMinutes(options.LockoutMinutes));
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogWarning("Failed sign-in for user {UserId}: invalid password", user.Id);
            return null;
        }

        user.RecordSuccessfulLogin(now);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var profile = UserMappings.ToProfile(user);
        var token = tokenService.CreateAccessToken(user, profile.Roles, profile.Permissions);
        return new LoginResponse(token.Token, token.ExpiresAt, profile);
    }

    public async Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken) ?? throw new NotFoundException("User", userId);
        return UserMappings.ToProfile(user);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken) ?? throw new NotFoundException("User", userId);

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new ValidationException(nameof(request.CurrentPassword), "Current password is incorrect.");
        }

        PasswordPolicy.Validate(request.NewPassword, nameof(request.NewPassword));
        user.ChangePasswordHash(passwordHasher.Hash(request.NewPassword));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
