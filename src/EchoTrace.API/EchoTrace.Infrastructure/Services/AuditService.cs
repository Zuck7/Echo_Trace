using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EchoTrace.Application.Common.Interfaces;
using EchoTrace.Domain.Entities;
using EchoTrace.Domain.Interfaces.Repositories;

namespace EchoTrace.Infrastructure.Services;

public class AuditService(
    IAuditLogRepository auditRepo,
    ICurrentTenantService tenant) : IAuditService
{
    private const string GenesisHash = "ECHO_TRACE_GENESIS";

    public async Task LogAsync(
        string entityType,
        string entityId,
        string action,
        object? oldValue,
        object? newValue,
        CancellationToken ct = default)
    {
        var lastEntry = await auditRepo.GetLastEntryForEntityAsync(entityType, entityId, ct);
        var previousHash = lastEntry?.ChainHash ?? ComputeHash(GenesisHash);

        var newValueJson = newValue is null ? null : JsonSerializer.Serialize(newValue);
        var oldValueJson = oldValue is null ? null : JsonSerializer.Serialize(oldValue);

        var chainHash = ComputeChainHash(previousHash, entityType, entityId, action, newValueJson, DateTime.UtcNow);

        var entry = AuditLogEntry.Create(
            tenant.TenantId,
            entityType,
            entityId,
            action,
            oldValueJson,
            newValueJson,
            tenant.UserId == Guid.Empty ? null : tenant.UserId,
            tenant.IpAddress,
            chainHash);

        await auditRepo.AppendAsync(entry, ct);
    }

    public static string ComputeChainHash(
        string previousHash,
        string entityType,
        string entityId,
        string action,
        string? newValue,
        DateTime timestamp)
    {
        var data = $"{previousHash}{entityType}{entityId}{action}{newValue}{timestamp:O}";
        return ComputeHash(data);
    }

    public static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
