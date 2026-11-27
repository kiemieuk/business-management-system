using BMS.BuildingBlocks.Domain.Exceptions;

namespace BMS.Identity.Application.Security;

/// <summary>
/// Password complexity rules applied whenever a password is set.
/// </summary>
public static class PasswordPolicy
{
    public const int MinimumLength = 8;
    public const int MaximumLength = 128;

    public static void Validate(string? password, string fieldName = "password")
    {
        var errors = new List<string>();
        if (string.IsNullOrEmpty(password) || password.Length < MinimumLength)
        {
            errors.Add($"Password must be at least {MinimumLength} characters long.");
        }
        else
        {
            if (password.Length > MaximumLength)
            {
                errors.Add($"Password must be at most {MaximumLength} characters long.");
            }

            if (!password.Any(char.IsUpper))
            {
                errors.Add("Password must contain an uppercase letter.");
            }

            if (!password.Any(char.IsLower))
            {
                errors.Add("Password must contain a lowercase letter.");
            }

            if (!password.Any(char.IsDigit))
            {
                errors.Add("Password must contain a digit.");
            }
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(new Dictionary<string, string[]> { [fieldName] = [.. errors] });
        }
    }
}
