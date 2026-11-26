using BMS.BuildingBlocks.Domain.Exceptions;
using BMS.Identity.Application.Security;

namespace BMS.Identity.Tests.Unit;

public class PasswordPolicyTests
{
    [Theory]
    [InlineData("")]
    [InlineData("Short1")]
    [InlineData("alllowercase1")]
    [InlineData("ALLUPPERCASE1")]
    [InlineData("NoDigitsHere")]
    public void Rejects_weak_passwords(string password)
    {
        var ex = Assert.Throws<ValidationException>(() => PasswordPolicy.Validate(password));
        Assert.True(ex.Errors.ContainsKey("password"));
    }

    [Fact]
    public void Accepts_strong_password()
    {
        PasswordPolicy.Validate("Str0ngPassword");
    }
}
