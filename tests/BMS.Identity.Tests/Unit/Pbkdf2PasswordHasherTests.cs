using BMS.Identity.Application.Security;

namespace BMS.Identity.Tests.Unit;

public class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Hash_does_not_contain_plain_password_and_verifies()
    {
        var hash = _hasher.Hash("Secret123");

        Assert.DoesNotContain("Secret123", hash);
        Assert.StartsWith($"PBKDF2-SHA512${Pbkdf2PasswordHasher.DefaultIterations}$", hash);
        Assert.True(_hasher.Verify("Secret123", hash));
    }

    [Fact]
    public void Verify_fails_for_wrong_password()
    {
        var hash = _hasher.Hash("Secret123");

        Assert.False(_hasher.Verify("secret123", hash));
    }

    [Fact]
    public void Same_password_produces_different_hashes_because_of_random_salt()
    {
        Assert.NotEqual(_hasher.Hash("Secret123"), _hasher.Hash("Secret123"));
    }

    [Fact]
    public void Hashes_created_with_other_iteration_counts_still_verify()
    {
        var legacy = new Pbkdf2PasswordHasher(iterations: 1_000).Hash("Secret123");

        Assert.True(_hasher.Verify("Secret123", legacy));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("PBKDF2-SHA512$abc$AAAA$AAAA")]
    [InlineData("PBKDF2-SHA512$1000$***$AAAA")]
    [InlineData("MD5$1000$AAAA$AAAA")]
    public void Verify_returns_false_for_malformed_hashes(string hash)
    {
        Assert.False(_hasher.Verify("Secret123", hash));
    }
}
