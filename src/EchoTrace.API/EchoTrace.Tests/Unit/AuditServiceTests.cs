using EchoTrace.Infrastructure.Services;
using FluentAssertions;

namespace EchoTrace.Tests.Unit;

public class AuditServiceTests
{
    [Fact]
    public void ComputeHash_is_deterministic_for_the_same_input()
    {
        AuditService.ComputeHash("same-input").Should().Be(AuditService.ComputeHash("same-input"));
    }

    [Fact]
    public void ComputeChainHash_changes_if_the_previous_hash_changes()
    {
        var timestamp = DateTime.UtcNow;

        var hashA = AuditService.ComputeChainHash("hash-A", "Organization", "1", "CREATE", "{}", timestamp);
        var hashB = AuditService.ComputeChainHash("hash-B", "Organization", "1", "CREATE", "{}", timestamp);

        hashA.Should().NotBe(hashB);
    }

    [Fact]
    public void ComputeChainHash_changes_if_the_payload_is_tampered_with()
    {
        var timestamp = DateTime.UtcNow;
        const string previousHash = "genesis";

        var original = AuditService.ComputeChainHash(previousHash, "Organization", "1", "CREATE", "{\"status\":\"Active\"}", timestamp);
        var tampered = AuditService.ComputeChainHash(previousHash, "Organization", "1", "CREATE", "{\"status\":\"Suspended\"}", timestamp);

        original.Should().NotBe(tampered);
    }
}
