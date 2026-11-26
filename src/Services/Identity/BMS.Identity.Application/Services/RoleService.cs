using BMS.BuildingBlocks.Contracts.Authorization;
using BMS.BuildingBlocks.Domain.Abstractions;
using BMS.BuildingBlocks.Domain.Exceptions;
using BMS.Identity.Application.Contracts;
using BMS.Identity.Domain.Roles;

namespace BMS.Identity.Application.Services;

public interface IRoleService
{
    Task<IReadOnlyList<RoleDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<RoleDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<RoleDto> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);

    Task<RoleDto> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken = default);

    Task<RoleDto> SetPermissionsAsync(Guid id, SetRolePermissionsRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    IReadOnlyList<PermissionDto> ListPermissions();
}

public sealed class RoleService(IRoleRepository roles, IUnitOfWork unitOfWork) : IRoleService
{
    public async Task<IReadOnlyList<RoleDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var all = await roles.ListAsync(cancellationToken);
        var counts = await roles.CountUsersPerRoleAsync(cancellationToken);
        return [.. all.OrderBy(r => r.Name).Select(r => ToDto(r, counts.GetValueOrDefault(r.Id)))];
    }

    public async Task<RoleDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await LoadAsync(id, cancellationToken);
        return ToDto(role, await roles.CountUsersAsync(id, cancellationToken));
    }

    public async Task<RoleDto> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        if (await roles.NameExistsAsync(request.Name, null, cancellationToken))
        {
            throw new ConflictException($"Role '{request.Name}' already exists.");
        }

        var role = Role.Create(request.Name, request.Description);
        role.SetPermissions(ValidatePermissions(request.Permissions ?? []));

        await roles.AddAsync(role, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(role, 0);
    }

    public async Task<RoleDto> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = await LoadAsync(id, cancellationToken);
        if (await roles.NameExistsAsync(request.Name, id, cancellationToken))
        {
            throw new ConflictException($"Role '{request.Name}' already exists.");
        }

        role.Update(request.Name, request.Description);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<RoleDto> SetPermissionsAsync(Guid id, SetRolePermissionsRequest request, CancellationToken cancellationToken = default)
    {
        var role = await LoadAsync(id, cancellationToken);
        if (role.Name == Role.AdministratorRoleName)
        {
            throw new DomainException("The Administrator role always holds every permission and cannot be modified.");
        }

        role.SetPermissions(ValidatePermissions(request.Permissions));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await LoadAsync(id, cancellationToken);
        role.EnsureCanBeDeleted();

        var userCount = await roles.CountUsersAsync(id, cancellationToken);
        if (userCount > 0)
        {
            throw new ConflictException($"Role '{role.Name}' is assigned to {userCount} user(s). Unassign it before deleting.");
        }

        roles.Remove(role);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public IReadOnlyList<PermissionDto> ListPermissions() =>
        [.. Permissions.All.Select(p => new PermissionDto(p.Code, p.Name, p.Group, p.Description))];

    private async Task<Role> LoadAsync(Guid id, CancellationToken cancellationToken) =>
        await roles.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Role", id);

    private static IReadOnlyList<string> ValidatePermissions(IReadOnlyList<string> codes)
    {
        var unknown = codes.Where(c => !Permissions.IsDefined(c)).ToList();
        if (unknown.Count > 0)
        {
            throw new ValidationException("Permissions", $"Unknown permission(s): {string.Join(", ", unknown)}.");
        }

        return codes;
    }

    private static RoleDto ToDto(Role role, int userCount) => new(
        role.Id,
        role.Name,
        role.Description,
        role.IsSystem,
        [.. role.Permissions.Select(p => p.PermissionCode).Order()],
        userCount);
}
