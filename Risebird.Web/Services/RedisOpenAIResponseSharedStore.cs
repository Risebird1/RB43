using Microsoft.Extensions.Caching.Distributed;
using Risebird.CommonHelper;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.Web.Services
{
    /// <summary>
    /// Puts the OpenAI response cache's second level in the app's Redis (the IDistributedCache that
    /// also holds session). Used only when OpenAI:CostOptimization:ResponseCache:UseRedis is on.
    /// An entry is dropped once it has gone unread for the idle window (sliding expiry, so each
    /// read keeps it alive) and in any case at its absolute expiry.
    /// </summary>
    public sealed class RedisOpenAIResponseSharedStore : IOpenAIResponseSharedStore
    {
        private readonly IDistributedCache _cache;

        public RedisOpenAIResponseSharedStore(IDistributedCache cache)
        {
            _cache = cache;
        }

        public Task<string> GetAsync(string key, CancellationToken cancellationToken) =>
            _cache.GetStringAsync(key, cancellationToken);

        public Task SetAsync(string key, string value, TimeSpan absoluteExpiry, TimeSpan idleExpiry,
            CancellationToken cancellationToken) =>
            _cache.SetStringAsync(key, value,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = absoluteExpiry,
                    SlidingExpiration = idleExpiry
                },
                cancellationToken);
    }
}
