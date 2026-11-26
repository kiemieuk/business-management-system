using System.ComponentModel.DataAnnotations;
using BMS.Identity.Domain.Users;

namespace BMS.Identity.Application.Contracts;

public sealed record RoleSummaryDto(Guid Id, string Name);

public sealed record UserDto(
    Guid Id,
    string Username,
    string Email,
    string FullName,
    bool IsActive,
    IReadOnlyList<RoleSummaryDto> Roles,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt);

public sealed record CreateUserRequest(
    [Required, StringLength(User.UsernameMaxLength, MinimumLength = 3)]
    [RegularExpression("^[a-zA-Z0-9._-]+$", ErrorMessage = "Username may only contain letters, digits, '.', '_' and '-'.")]
    string Username,
    [Required, EmailAddress, StringLength(User.EmailMaxLength)] string Email,
    [Required, StringLength(User.FullNameMaxLength)] string FullName,
    [Required] string Password,
    IReadOnlyList<Guid>? RoleIds);

public sealed record UpdateUserRequest(
    [Required, EmailAddress, StringLength(User.EmailMaxLength)] string Email,
    [Required, StringLength(User.FullNameMaxLength)] string FullName,
    bool IsActive);

public sealed record AssignRolesRequest([Required] IReadOnlyList<Guid> RoleIds);

public sealed record ResetPasswordRequest([Required] string NewPassword);
