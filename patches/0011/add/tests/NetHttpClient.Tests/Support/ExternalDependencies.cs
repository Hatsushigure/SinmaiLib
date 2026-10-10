using System.Collections.Concurrent;
using System.Net;

// Only the game runtime dependencies are replaced. NetHttpClient, CipherAes and
// Singleton are compiled from their production source files.
namespace AMDaemon.Allnet
{
    internal static class Auth
    {
        public static string GameServerHost => "localhost";
    }
}

namespace Manager
{
    internal sealed class OperationManager
    {
        private readonly ConcurrentDictionary<ulong, CookieContainer> _cookies = new();

        public bool IsHttpConnection => true;

        public CookieContainer? GetCookie(ulong userId) =>
            _cookies.GetValueOrDefault(userId);

        public void SetCookie(ulong userId, CookieContainer cookies) => _cookies[userId] = cookies;

        public void RemoveCookie(ulong userId) => _cookies.TryRemove(userId, out _);
    }
}
