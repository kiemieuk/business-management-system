using BMS.BuildingBlocks.Domain.Abstractions;
using BMS.BuildingBlocks.Domain.Exceptions;
using BMS.Identity.Application.Abstractions;
using BMS.Identity.Application.Contracts;
using BMS.Identity.Application.Security;
using BMS.Identity.Domain.Roles;
using BMS.Identity.Domain.Users;

namespace BMS.Identity.Application.Services;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<UserDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default);

    Task<UserDto> AssignRolesAsync(Guid id, AssignRolesRequest request, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(Guid id, ResetPasswordRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class UserService(
    IUserRepository users,
    IRoleRepository roles,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ICurrentUser currentUser) : IUserService
{
    public async Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken = default) =>
        [.. (await users.ListAsync(cancellationToken)).OrderBy(u => u.Username).Select(UserMappings.ToDto)];

    public async Task<UserDto> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        UserMappings.ToDto(await LoadAsync(id, cancellationToken));

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        PasswordPolicy.Validate(request.Password, nameof(request.Password));

        if (await users.UsernameExistsAsync(request.Username, cancellationToken))
        {
            throw new ConflictException($"Username '{request.Username}' is already taken.");
        }

        if (await users.EmailExistsAsync(request.Email, null, cancellationToken))
        {
            throw new ConflictException($"Email '{request.Email}' is already in use.");
        }

        var roleList = await LoadRolesAsync(request.RoleIds ?? [], cancellationToken);
        await EnsureCanGrantRolesAsync(roleList, cancellationToken);

        var user = User.Create(request.Username, request.Email, request.FullName, passwordHasher.Hash(request.Password));
        user.SetRoles(roleList);

        await users.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetAsync(user.Id, cancellationToken);
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await LoadAsync(id, cancellationToken);
        await EnsureCanManageAsync(user, cancellationToken);

        if (await users.EmailExistsAsync(request.Email, id, cancellationToken))
        {
            throw new ConflictException($"Email '{request.Email}' is already in use.");
        }

        if (!request.IsActive && user.IsActive)
        {
            if (currentUser.UserId == id)
            {
                throw new DomainException("You cannot deactivate your own account.");
            }

            await EnsureNotLastAdministratorAsync(user, cancellationToken);
        }

        user.UpdateProfile(request.Email, request.FullName);
        if (request.IsActive)
        {
            user.Activate();
        }
        else
        {
            user.Deactivate();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return UserMappings.ToDto(user);
    }

    public async Task<UserDto> AssignRolesAsync(Guid id, AssignRolesRequest request, CancellationToken cancellationToken = default)
    {
        var user = await LoadAsync(id, cancellationToken);
        await EnsureCanManageAsync(user, cancellationToken);
        var newRoles = await LoadRolesAsync(request.RoleIds, cancellationToken);
        await EnsureCanGrantRolesAsync(newRoles, cancellationToken);

        if (!newRoles.Any(r => r.Name == Role.AdministratorRoleName))
        {
            await EnsureNotLastAdministratorAsync(user, cancellationToken);
        }

        user.SetRoles(newRoles);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task ResetPasswordAsync(Guid id, ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await LoadAsync(id, cancellationToken);
        await EnsureCanManageAsync(user, cancellationToken);
        PasswordPolicy.Validate(request.NewPassword, nameof(request.NewPassword));
        user.ChangePasswordHash(passwordHasher.Hash(request.NewPassword));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await LoadAsync(id, cancellationToken);
        if (currentUser.UserId == id)
        {
            throw new DomainException("You cannot delete your own account.");
        }

        await EnsureCanManageAsync(user, cancellationToken);

        await EnsureNotLastAdministratorAsync(user, cancellationToken);
        users.Remove(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> LoadAsync(Guid id, CancellationToken cancellationToken) =>
        await users.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("User", id);

    private async Task<IReadOnlyList<Role>> LoadRolesAsync(IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken)
    {
        var distinct = roleIds.Distinct().ToList();
        var found = await roles.GetByIdsAsync(distinct, cancellationToken);
        var missing = distinct.Except(found.Select(r => r.Id)).ToList();
        if (missing.Count > 0)
        {
            throw new ValidationException("RoleIds", $"Unknown role(s): {string.Join(", ", missing)}.");
        }

        return found;
    }

    private static bool IsAdministrator(User user) =>
        user.UserRoles.Any(ur => ur.Role.Name == Role.AdministratorRoleName);

    /// <summary>
    /// Checks the caller's current roles in the database (not the possibly stale token claims).
    /// </summary>
    private async Task<bool> CurrentUserIsAdministratorAsync(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } actorId)
        {
            return false;
        }

        var actor = await users.GetByIdAsync(actorId, cancellationToken);
        return actor is { IsActive: true } && IsAdministrator(actor);
    }

    /// <summary>
    /// Prevents privilege escalation: only administrators may manage administrator accounts
    /// (edit, reset password, change roles, delete).
    /// </summary>
    private async Task EnsureCanManageAsync(User target, CancellationToken cancellationToken)
    {
        if (IsAdministrator(target) && !await CurrentUserIsAdministratorAsync(cancellationToken))
        {
            throw new ForbiddenException("Only administrators can manage administrator accounts.");
        }
    }

    /// <summary>
    /// Prevents privilege escalation: only administrators may grant the Administrator role.
    /// </summary>
    private async Task EnsureCanGrantRolesAsync(IEnumerable<Role> rolesToGrant, CancellationToken cancellationToken)
    {
        if (rolesToGrant.Any(r => r.Name == Role.AdministratorRoleName) && !await CurrentUserIsAdministratorAsync(cancellationToken))
        {
            throw new ForbiddenException("Only administrators can grant the Administrator role.");
        }
    }

    /// <summary>
    /// Prevents the system from being left without any active administrator.
    /// </summary>
    private async Task EnsureNotLastAdministratorAsync(User user, CancellationToken cancellationToken)
    {
        var isAdministrator = user.IsActive && user.UserRoles.Any(ur => ur.Role.Name == Role.AdministratorRoleName);
        if (isAdministrator && await users.CountActiveUsersInRoleAsync(Role.AdministratorRoleName, user.Id, cancellationToken) == 0)
        {
            throw new DomainException("At least one active user must keep the Administrator role.");
        }
    }
}
