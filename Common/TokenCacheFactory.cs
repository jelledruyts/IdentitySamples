using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Collections.Generic;
using System.Security.Claims;

namespace Common
{
    public static class TokenCacheFactory
    {
        // Use static in-memory token caches to keep it simple, but for security and scalability reasons
        // make sure to use a separate token cache per user (token cache lookups are not designed to work
        // across the millions of users a web application could have).
        // In real-world applications this should of course not be stored in memory but in e.g. session state
        // or a database.
        private static Dictionary<string, TokenCache> tokenCachePerUserId = new Dictionary<string, TokenCache>();

        public static TokenCache GetTokenCacheForCurrentPrincipal()
        {
            var userId = ClaimsPrincipal.Current.GetUniqueIdentifier();
            return GetTokenCache(userId);
        }

        public static void DeleteTokenCacheForCurrentPrincipal()
        {
            var userId = ClaimsPrincipal.Current.GetUniqueIdentifier();
            DeleteTokenCache(userId);
        }

        public static TokenCache GetTokenCache(string userId)
        {
            if (!tokenCachePerUserId.ContainsKey(userId))
            {
                tokenCachePerUserId[userId] = new TokenCache();
            }
            return tokenCachePerUserId[userId];
        }

        public static void DeleteTokenCache(string userId)
        {
            tokenCachePerUserId.Remove(userId);
        }
    }
}