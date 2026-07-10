using Unity.Netcode;

using SplitRun.Constants;

namespace SplitRun.Network
{
    public enum SessionRole
    {
        All          = 0,
        LaneOnly     = 1,
        VerticalOnly = 2,
    }

    public static class SessionRoleResolver
    {
        public static SessionRole Resolve()
        {
            NetworkManager networkManager = NetworkManager.Singleton;

            if (!networkManager || !networkManager.IsListening) return SessionRole.All;
            if (!networkManager.IsHost) return SessionRole.VerticalOnly;

            return networkManager.ConnectedClients.Count >= NetworkConstants.k_SessionPlayerCount
                ? SessionRole.LaneOnly
                : SessionRole.All;
        }
    }
}
