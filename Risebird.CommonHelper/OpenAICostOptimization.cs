using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Specialized;
using System.Runtime.Caching;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    /// <summary>
    /// Reads <c>OpenAI:CostOptimization</c>, the switches that let a single operation run on a cheaper
    /// model or reuse an earlier identical answer. Every switch is off unless configured, and the
    /// whole block is behind <c>Enabled</c> so it can be turned off in one place without a deploy.
    /// Values are read on every call, so a configuration reload takes effect immediately.
    ///
    /// <code>
    /// "CostOptimization": {
    ///   "Enabled": true,
    ///   "ModelOverrides": { "TranscriptToNotes": "gpt-4.1-mini" },
    ///   "FallbackToDefaultModelOnFailure": true,
    ///   "UsePromptCacheKey": true,
    ///   "ResponseCache": {
    ///     "Enabled": true,
    ///     "MemoryLimitMegabytes": 64,
    ///     "UseRedis": true,
    ///     "Operations": { "ResumeAnalysis": 1440 }
    ///   }
    /// }
    /// </code>
    /// Keys under <c>ModelOverrides</c> and <c>ResponseCache:Operations</c> are the operation types
    /// written to OpenAIApiLogs.OperationType.
    /// </summary>
    public sealed class OpenAICostOptimization
    {
        public const string SectionName = "OpenAI:CostOptimization";

        /// <summary>A cached answer older than a week is not worth the risk of serving stale output.</summary>
        public const int MaxCacheTtlMinutes = 7 * 24 * 60;

        private readonly IConfiguration _configuration;

        public OpenAICostOptimization(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool Enabled => _configuration.GetValue($"{SectionName}:Enabled", false);

        /// <summary>
        /// The model configured for <paramref name="operationType"/>, or null when the operation keeps
        /// the default model.
        /// </summary>
        public string ModelOverrideFor(string operationType)
        {
            if (!Enabled || string.IsNullOrWhiteSpace(operationType))
                return null;

            var model = _configuration[$"{SectionName}:ModelOverrides:{operationType}"];
            return string.IsNullOrWhiteSpace(model) ? null : model.Trim();
        }

        /// <summary>
        /// When an optimised request fails or (for JSON operations) returns unparseable output, repeat it
        /// exactly as it would have been sent without optimisation. On by default: a cheaper model that
        /// cannot do the job must cost a retry, never a missing report.
        /// </summary>
        public bool FallbackToDefaultModelOnFailure =>
            _configuration.GetValue($"{SectionName}:FallbackToDefaultModelOnFailure", true);

        /// <summary>
        /// Sends <c>prompt_cache_key</c> so requests sharing a stored prompt are routed to the same
        /// OpenAI cache and bill their repeated prefix at the cached-input rate. Output is unaffected.
        /// </summary>
        public bool UsePromptCacheKey =>
            Enabled && _configuration.GetValue($"{SectionName}:UsePromptCacheKey", false);

        /// <summary>
        /// How long an identical request's answer may be reused for <paramref name="operationType"/>,
        /// or null when the operation is not cached. Only operations listed under
        /// <c>ResponseCache:Operations</c> are ever cached.
        /// </summary>
        public int? CacheTtlMinutesFor(string operationType)
        {
            if (!Enabled || string.IsNullOrWhiteSpace(operationType)
                || !_configuration.GetValue($"{SectionName}:ResponseCache:Enabled", false))
                return null;

            var ttl = _configuration.GetValue<int?>($"{SectionName}:ResponseCache:Operations:{operationType}");
            if (!ttl.HasValue || ttl.Value <= 0)
                return null;

            return Math.Min(ttl.Value, MaxCacheTtlMinutes);
        }

        public int CacheMemoryLimitMegabytes =>
            Math.Max(1, _configuration.GetValue($"{SectionName}:ResponseCache:MemoryLimitMegabytes", 64));

        /// <summary>
        /// How cached answers may use the store shared by every instance (Redis in the web app). Off
        /// unless <c>ResponseCache:UseRedis</c> is true; only consulted for operations that are cached.
        /// Only repeated requests reach Redis, and an entry left unread for <c>RedisIdleMinutes</c>
        /// (60 by default) is dropped. Answers over <c>RedisMaxEntryKilobytes</c> stay in process, and a
        /// Redis call slower than <c>RedisTimeoutMilliseconds</c> is abandoned.
        /// </summary>
        public OpenAIResponseSharedStorePolicy SharedStorePolicy => new OpenAIResponseSharedStorePolicy
        {
            Enabled = _configuration.GetValue($"{SectionName}:ResponseCache:UseRedis", false),
            MaxEntryKilobytes = Math.Max(1, _configuration.GetValue($"{SectionName}:ResponseCache:RedisMaxEntryKilobytes", 512)),
            TimeoutMilliseconds = Math.Clamp(_configuration.GetValue($"{SectionName}:ResponseCache:RedisTimeoutMilliseconds", 500), 50, 5000),
            IdleMinutes = Math.Clamp(_configuration.GetValue($"{SectionName}:ResponseCache:RedisIdleMinutes", 60), 5, 24 * 60)
        };

        /// <summary>A short stable key per operation and stored prompt, within OpenAI's length limit.</summary>
        public static string PromptCacheKey(string operationType, string promptId) =>
            "rb-" + Hash($"{operationType}|{promptId}").Substring(0, 32);

        internal static string Hash(string value)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
            var builder = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                builder.Append(b.ToString("x2"));
            return builder.ToString();
        }
    }

    /// <summary>
    /// A cache shared by every instance, behind the in-process store. The web app plugs Redis in here
    /// (see <see cref="OpenAIResponseCache.UseSharedStore"/>); processes that don't keep the in-process
    /// store only. Kept as a two-method interface so this project needs no caching package.
    /// </summary>
    public interface IOpenAIResponseSharedStore
    {
        Task<string> GetAsync(string key, CancellationToken cancellationToken);

        /// <summary>
        /// Stores <paramref name="value"/> until <paramref name="absoluteExpiry"/> from now, or earlier
        /// once it has gone unread for <paramref name="idleExpiry"/>.
        /// </summary>
        Task SetAsync(string key, string value, TimeSpan absoluteExpiry, TimeSpan idleExpiry, CancellationToken cancellationToken);
    }

    /// <summary>How the shared store (Redis) may be used; read from configuration on every call.</summary>
    public sealed class OpenAIResponseSharedStorePolicy
    {
        public static readonly OpenAIResponseSharedStorePolicy Off = new OpenAIResponseSharedStorePolicy();

        public bool Enabled { get; set; }
        public int MaxEntryKilobytes { get; set; } = 512;
        public int TimeoutMilliseconds { get; set; } = 500;
        public int IdleMinutes { get; set; } = 60;
    }

    /// <summary>
    /// Store of successful OpenAI answers, keyed by a hash of the exact request body. Any difference
    /// in model, prompt, prompt version, variables, input or output format is a different key, so a
    /// hit is always an answer to the identical question.
    ///
    /// The first level is in-process. It is deliberately not taken from dependency injection: the
    /// helper runs in the web app, the timer functions and the microservice, and only the web app
    /// registers a memory cache.
    ///
    /// When a shared store is attached and <c>ResponseCache:UseRedis</c> is on, Redis holds only
    /// answers that have actually been asked for more than once: the first time a request is seen,
    /// only a small "seen" marker is written (kept for <c>RedisIdleMinutes</c>); the answer goes to
    /// Redis when the same request comes again, on any instance, within that window. A Redis entry
    /// is dropped once it has gone unread for <c>RedisIdleMinutes</c> (an hour by default), and in
    /// any case when the operation's TTL runs out. The shared store is best-effort: a slow or failing
    /// Redis costs at most the configured timeout, then it is skipped for a minute and requests go to
    /// OpenAI as usual.
    /// </summary>
    public static class OpenAIResponseCache
    {
        private const string SeenMarkerSuffix = ":seen";
        private static readonly object InitLock = new object();
        private static readonly TimeSpan SharedStorePause = TimeSpan.FromMinutes(1);
        private static MemoryCache _cache;
        private static IOpenAIResponseSharedStore _sharedStore;
        private static long _sharedStorePausedUntilTicks;

        public sealed class Entry
        {
            public string OutputText { get; set; }
            public string ResponseId { get; set; }
            public string Model { get; set; }
            public decimal OriginalCostUSD { get; set; }
            public DateTimeOffset ExpiresAtUtc { get; set; }

            /// <summary>"memory" or "redis": where this hit came from. Not stored.</summary>
            [JsonIgnore]
            public string Source { get; set; }

            /// <summary>Whether this answer has been written to (or read from) the shared store. Not stored.</summary>
            [JsonIgnore]
            public bool InSharedStore { get; set; }

            internal Entry AsHitFrom(string source) => new Entry
            {
                OutputText = OutputText,
                ResponseId = ResponseId,
                Model = Model,
                OriginalCostUSD = OriginalCostUSD,
                ExpiresAtUtc = ExpiresAtUtc,
                InSharedStore = InSharedStore,
                Source = source
            };
        }

        public static string Key(string kind, string operationType, string requestBody) =>
            $"oai:{kind}:{operationType}:{OpenAICostOptimization.Hash(requestBody)}";

        /// <summary>Attaches (or with null, detaches) the store shared across instances.</summary>
        public static void UseSharedStore(IOpenAIResponseSharedStore store)
        {
            _sharedStore = store;
            Interlocked.Exchange(ref _sharedStorePausedUntilTicks, 0);
        }

        public static bool TryGet(string key, int memoryLimitMegabytes, out Entry entry)
        {
            entry = (Store(memoryLimitMegabytes).Get(key) as Entry)?.AsHitFrom("memory");
            return entry != null;
        }

        /// <summary>
        /// The cached answer for <paramref name="key"/>, from this process or, when the policy allows,
        /// from the shared store; null on a miss. A hit in this process is itself proof of a repeat, so
        /// that answer is copied to the shared store the first time it is reused.
        /// A shared-store failure is reported to <paramref name="onSharedStoreError"/> and treated as a miss.
        /// </summary>
        public static async Task<Entry> GetAsync(string key, int memoryLimitMegabytes,
            OpenAIResponseSharedStorePolicy policy, Action<Exception> onSharedStoreError = null)
        {
            policy ??= OpenAIResponseSharedStorePolicy.Off;
            var cached = Store(memoryLimitMegabytes).Get(key) as Entry;
            if (cached != null)
            {
                if (!cached.InSharedStore)
                    await WriteAnswerToSharedStoreAsync(key, cached, policy, onSharedStoreError);
                return cached.AsHitFrom("memory");
            }

            var shared = SharedStoreIfAvailable(policy);
            if (shared == null)
                return null;

            try
            {
                var json = await WithTimeout(ct => shared.GetAsync(key, ct), policy.TimeoutMilliseconds);
                if (string.IsNullOrWhiteSpace(json))
                    return null;

                var entry = JsonConvert.DeserializeObject<Entry>(json);
                if (entry == null || string.IsNullOrWhiteSpace(entry.OutputText) || entry.ExpiresAtUtc <= DateTimeOffset.UtcNow)
                    return null;

                entry.InSharedStore = true;
                Store(memoryLimitMegabytes).Set(key, entry, entry.ExpiresAtUtc);
                return entry.AsHitFrom("redis");
            }
            catch (Exception ex)
            {
                PauseSharedStore(ex, onSharedStoreError);
                return null;
            }
        }

        public static void Set(string key, Entry entry, int ttlMinutes, int memoryLimitMegabytes)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.OutputText) || ttlMinutes <= 0)
                return;

            entry.ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(ttlMinutes);
            Store(memoryLimitMegabytes).Set(key, entry, entry.ExpiresAtUtc);
        }

        /// <summary>
        /// Caches a fresh answer in this process. With the shared store on, it also goes to Redis if
        /// this request was already seen (on any instance) within the idle window; otherwise only the
        /// small "seen" marker is written, so one-off requests never occupy Redis.
        /// </summary>
        public static async Task SetAsync(string key, Entry entry, int ttlMinutes, int memoryLimitMegabytes,
            OpenAIResponseSharedStorePolicy policy, Action<Exception> onSharedStoreError = null)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.OutputText) || ttlMinutes <= 0)
                return;

            policy ??= OpenAIResponseSharedStorePolicy.Off;
            Set(key, entry, ttlMinutes, memoryLimitMegabytes);

            var shared = SharedStoreIfAvailable(policy);
            if (shared == null)
                return;

            try
            {
                var seenBefore = await WithTimeout(ct => shared.GetAsync(key + SeenMarkerSuffix, ct), policy.TimeoutMilliseconds);
                if (string.IsNullOrEmpty(seenBefore))
                {
                    var window = TimeSpan.FromMinutes(policy.IdleMinutes);
                    await WithTimeout(async ct =>
                    {
                        await shared.SetAsync(key + SeenMarkerSuffix, "1", window, window, ct);
                        return true;
                    }, policy.TimeoutMilliseconds);
                    return;
                }
            }
            catch (Exception ex)
            {
                PauseSharedStore(ex, onSharedStoreError);
                return;
            }

            await WriteAnswerToSharedStoreAsync(key, entry, policy, onSharedStoreError);
        }

        /// <summary>Drops every answer cached in this process. For tests and for an operator who changed a prompt.</summary>
        public static void Clear()
        {
            lock (InitLock)
            {
                _cache?.Dispose();
                _cache = null;
            }
        }

        private static async Task WriteAnswerToSharedStoreAsync(string key, Entry entry,
            OpenAIResponseSharedStorePolicy policy, Action<Exception> onSharedStoreError)
        {
            var shared = SharedStoreIfAvailable(policy);
            if (shared == null)
                return;

            var remaining = entry.ExpiresAtUtc - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero)
                return;

            var json = JsonConvert.SerializeObject(entry);
            if (Encoding.UTF8.GetByteCount(json) > policy.MaxEntryKilobytes * 1024L)
            {
                // Too big for Redis: remember that, so every later memory hit does not re-check.
                entry.InSharedStore = true;
                return;
            }

            try
            {
                var idle = TimeSpan.FromMinutes(policy.IdleMinutes);
                await WithTimeout(async ct =>
                {
                    await shared.SetAsync(key, json, remaining, idle < remaining ? idle : remaining, ct);
                    return true;
                }, policy.TimeoutMilliseconds);
                entry.InSharedStore = true;
            }
            catch (Exception ex)
            {
                PauseSharedStore(ex, onSharedStoreError);
            }
        }

        private static IOpenAIResponseSharedStore SharedStoreIfAvailable(OpenAIResponseSharedStorePolicy policy)
        {
            if (policy == null || !policy.Enabled)
                return null;
            if (DateTime.UtcNow.Ticks < Interlocked.Read(ref _sharedStorePausedUntilTicks))
                return null;
            return _sharedStore;
        }

        private static void PauseSharedStore(Exception ex, Action<Exception> onSharedStoreError)
        {
            Interlocked.Exchange(ref _sharedStorePausedUntilTicks, DateTime.UtcNow.Add(SharedStorePause).Ticks);
            onSharedStoreError?.Invoke(ex);
        }

        private static async Task<T> WithTimeout<T>(Func<CancellationToken, Task<T>> operation, int timeoutMilliseconds)
        {
            using var cts = new CancellationTokenSource();
            var task = operation(cts.Token);
            var finished = await Task.WhenAny(task, Task.Delay(Math.Max(1, timeoutMilliseconds), cts.Token));
            cts.Cancel();
            if (finished != task)
            {
                // Observe the abandoned call so a late failure is not an unobserved task exception.
                _ = task.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
                throw new TimeoutException($"Shared response cache did not answer within {timeoutMilliseconds} ms.");
            }
            return await task;
        }

        private static MemoryCache Store(int memoryLimitMegabytes)
        {
            var cache = _cache;
            if (cache != null)
                return cache;

            lock (InitLock)
            {
                // The limit is fixed when the store is first created; changing it needs a restart.
                return _cache ??= new MemoryCache("OpenAIResponseCache", new NameValueCollection
                {
                    ["CacheMemoryLimitMegabytes"] = memoryLimitMegabytes.ToString(),
                    ["PollingInterval"] = "00:02:00"
                });
            }
        }
    }
}
