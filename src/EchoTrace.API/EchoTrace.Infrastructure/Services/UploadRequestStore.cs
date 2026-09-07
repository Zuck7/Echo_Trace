using EchoTrace.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace EchoTrace.Infrastructure.Services;

public class UploadRequestStore(IMemoryCache cache) : IUploadRequestStore
{
    private static string CacheKey(Guid uploadRequestId) => $"upload-request:{uploadRequestId}";

    public void Save(PendingUpload upload, TimeSpan ttl) =>
        cache.Set(CacheKey(upload.UploadRequestId), upload, ttl);

    public PendingUpload? TryTake(Guid uploadRequestId)
    {
        var key = CacheKey(uploadRequestId);
        if (!cache.TryGetValue(key, out PendingUpload? pending)) return null;

        cache.Remove(key);
        return pending;
    }
}
