using Unity.Netcode;

using SplitRun.Constants;

namespace SplitRun.Network
{
    // Solo keeps every axis; the 2-player host owns lanes (P1), the client owns jump/slide (P2).
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
