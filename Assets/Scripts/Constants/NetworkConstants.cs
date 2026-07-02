namespace SplitRun.Constants
{
    public static class NetworkConstants
    {
        // Relay counts joiners only — a 2-player session is the host plus one connection.
        public const int k_MaxRelayConnections = 1;

        public const int k_SessionPlayerCount = 2;

        public const int    k_RelayRetryCount        = 3;
        public const float  k_RelayRetryDelaySeconds = 1f;
        public const string k_RelayConnectionType    = "dtls";
    }
}
