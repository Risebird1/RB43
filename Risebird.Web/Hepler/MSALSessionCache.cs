using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Client;
using System.Threading;


namespace Risebird.Web.Hepler
{
    public class MSALSessionCache
    {
        private static ReaderWriterLockSlim SessionLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        string UserId = string.Empty;
        string CacheId = string.Empty;
        HttpContext httpContext = null;
        byte[] cachedBytes;

        ITokenCache cache = new TokenCache();

        public MSALSessionCache(string userId, HttpContext httpcontext)
        {
            // not object, we want the SUB
            UserId = userId;
            CacheId = UserId + "_TokenCache";
            httpContext = httpcontext;
            Load();
        }

        public ITokenCache GetMsalCacheInstance()
        {
            cache.SetBeforeAccess(BeforeAccessNotification);
            cache.SetAfterAccess(AfterAccessNotification);
            return cache;
        }

        public void SaveUserStateValue(string state)
        {
            SessionLock.EnterWriteLock();
            httpContext.Session.SetString(CacheId + "_state", state);
            SessionLock.ExitWriteLock();
        }
        public string ReadUserStateValue()
        {
            SessionLock.EnterReadLock();
            string state = httpContext.Session.GetString(CacheId + "_state");
            SessionLock.ExitReadLock();
            return state;
        }
        public void Load()
        {
            SessionLock.EnterReadLock();
            cachedBytes = httpContext.Session.Get(CacheId);
            SessionLock.ExitReadLock();
        }

        public void Persist(byte[] bytes)
        {
            SessionLock.EnterWriteLock();
            cachedBytes = bytes;
            httpContext.Session.Set(CacheId, bytes);
            SessionLock.ExitWriteLock();
        }

        // Triggered right before MSAL needs to access the cache.
        // Reload the cache from the persistent store in case it changed since the last access.
        void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            Load();
            args.TokenCache.DeserializeMsalV3(cachedBytes);
        }

        // Triggered right after MSAL accessed the cache.
        void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            if (args.HasStateChanged)
            {
                Persist(args.TokenCache.SerializeMsalV3());
            }
        }
    }
}
