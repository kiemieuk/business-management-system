using System.ComponentModel.DataAnnotations;

namespace BMS.Identity.Application.Contracts;

public sealed record LoginRequest(
    [Required, StringLength(64)] string Username,
    [Required, StringLength(128)] string Password);

public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt, UserProfileDto User);

public sealed record UserProfileDto(
    Guid Id,
    string Username,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public sealed record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required] string NewPassword);
