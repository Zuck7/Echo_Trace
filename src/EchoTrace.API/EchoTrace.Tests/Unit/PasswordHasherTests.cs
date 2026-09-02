using EchoTrace.Infrastructure.Services;
using FluentAssertions;

namespace EchoTrace.Tests.Unit;

public class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new();

    [Fact]
    public void Verify_succeeds_for_the_correct_password()
    {
        var hash = _sut.Hash("SecureP@ss123");

        _sut.Verify("SecureP@ss123", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_fails_for_the_wrong_password()
    {
        var hash = _sut.Hash("SecureP@ss123");

        _sut.Verify("WrongPassword", hash).Should().BeFalse();
    }

    [Fact]
    public void Hashing_the_same_password_twice_yields_different_output()
    {
        var hash1 = _sut.Hash("SecureP@ss123");
        var hash2 = _sut.Hash("SecureP@ss123");

        hash1.Should().NotBe(hash2); // random salt per hash
        _sut.Verify("SecureP@ss123", hash1).Should().BeTrue();
        _sut.Verify("SecureP@ss123", hash2).Should().BeTrue();
    }
}
