using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace TodoListWebApi.Infrastructure
{
    public static class TokenCacheFactory
    {
        // Use a static in-memory token cache instance to keep it simple.
        private static TokenCache instance = new TokenCache();

        public static TokenCache Instance { get { return instance; } }
    }
}