using System;
using System.Threading;

using UnityEngine;
using UnityEngine.SceneManagement;

using Cysharp.Threading.Tasks;
using R3;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

using SplitRun.Constants;

namespace SplitRun.Network
{
    public class NetworkService : IDisposable
    {
        private readonly ReactiveProperty<NetworkConnectionState> _connectionState =
            new ReactiveProperty<NetworkConnectionState>(NetworkConnectionState.Offline);

        private readonly ReactiveProperty<string> _joinCode       = new ReactiveProperty<string>(string.Empty);
        private readonly ReactiveProperty<bool>   _isSessionReady = new ReactiveProperty<bool>(false);

        private bool _isSignedIn;
        private bool _isSigningIn;
        private bool _isSubscribed;

        public ReadOnlyReactiveProperty<NetworkConnectionState> ConnectionState => _connectionState;
        public ReadOnlyReactiveProperty<string>                 JoinCode        => _joinCode;
        public ReadOnlyReactiveProperty<bool>                   IsSessionReady  => _isSessionReady;

        public void Dispose()
        {
            ResetSession(NetworkConnectionState.Offline);

            _connectionState.Dispose();
            _joinCode.Dispose();
            _isSessionReady.Dispose();
        }

        /// <summary>Initializes Unity Services and signs in anonymously. Failure is non-fatal — multiplayer stays disabled.</summary>
        public async UniTask InitializeAsync(CancellationToken ct)
        {
            if (_isSignedIn || _isSigningIn) return;

            _isSigningIn = true;
            try
            {
                await UnityServices.InitializeAsync().AsUniTask().AttachExternalCancellation(ct);

                if (!AuthenticationService.Instance.IsSignedIn)
                    await AuthenticationService.Instance.SignInAnonymouslyAsync().AsUniTask().AttachExternalCancellation(ct);

                _isSignedIn = true;
                Debug.Log($"[NetworkService] Signed in — player id: {AuthenticationService.Instance.PlayerId}");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NetworkService] Sign-in failed — multiplayer disabled: {e.Message}");
            }
            finally
            {
                _isSigningIn = false;
            }
        }

        /// <summary>Allocates a Relay server, publishes the join code, and starts hosting.</summary>
        public async UniTask CreateRoomAsync(CancellationToken ct)
        {
            if (!await TryEnterConnectingAsync(ct)) return;

            try
            {
                Allocation allocation = await WithRelayRetryAsync(
                    () => RelayService.Instance.CreateAllocationAsync(NetworkConstants.k_MaxRelayConnections)
                        .AsUniTask().AttachExternalCancellation(ct), ct);

                string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId)
                    .AsUniTask().AttachExternalCancellation(ct);

                ConfigureTransport(AllocationUtils.ToRelayServerData(allocation, NetworkConstants.k_RelayConnectionType));
                StartHosting(joinCode);
            }
            catch (OperationCanceledException)
            {
                ResetSession(NetworkConnectionState.Offline);
                throw;
            }
            catch (Exception e)
            {
                Fail(e.Message);
            }
        }

        /// <summary>Joins an existing Relay allocation by code and starts a client.</summary>
        public async UniTask JoinRoomAsync(string joinCode, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(joinCode)) return;
            if (!await TryEnterConnectingAsync(ct)) return;

            try
            {
                JoinAllocation allocation = await WithRelayRetryAsync(
                    () => RelayService.Instance.JoinAllocationAsync(joinCode)
                        .AsUniTask().AttachExternalCancellation(ct), ct);

                ConfigureTransport(AllocationUtils.ToRelayServerData(allocation, NetworkConstants.k_RelayConnectionType));
                StartJoining();
            }
            catch (OperationCanceledException)
            {
                ResetSession(NetworkConnectionState.Offline);
                throw;
            }
            catch (Exception e)
            {
                Fail(e.Message);
            }
        }

        /// <summary>Shuts down any active session and returns to Offline.</summary>
        public void Disconnect() => ResetSession(NetworkConnectionState.Offline);

        private async UniTask<bool> TryEnterConnectingAsync(CancellationToken ct)
        {
            if (_connectionState.Value != NetworkConnectionState.Offline
                && _connectionState.Value != NetworkConnectionState.Failed)
                return false;

            // Sign-in is retried here so a device that was offline at boot can still go online later.
            await InitializeAsync(ct);
            if (!_isSignedIn)
            {
                Fail("Sign-in unavailable");
                return false;
            }

            if (!NetworkManager.Singleton)
            {
                Fail("No NetworkManager in the scene");
                return false;
            }

            _connectionState.Value = NetworkConnectionState.Connecting;
            return true;
        }

        private static async UniTask<T> WithRelayRetryAsync<T>(Func<UniTask<T>> request, CancellationToken ct)
        {
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    return await request();
                }
                catch (RelayServiceException e) when (IsTransient(e) && attempt < NetworkConstants.k_RelayRetryCount)
                {
                    Debug.LogWarning($"[NetworkService] Relay attempt {attempt} failed — retrying: {e.Message}");
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(NetworkConstants.k_RelayRetryDelaySeconds), cancellationToken: ct);
                }
            }
        }

        // A bad join code or malformed request never succeeds — retrying only delays the Failed state.
        private static bool IsTransient(RelayServiceException e) => e.Reason switch
        {
            RelayExceptionReason.JoinCodeNotFound => false,
            RelayExceptionReason.EntityNotFound   => false,
            RelayExceptionReason.InvalidRequest   => false,
            RelayExceptionReason.InvalidArgument  => false,
            _                                     => true,
        };

        private static void ConfigureTransport(RelayServerData relayData)
        {
            // UnityTransport lives on the NetworkManager object — NGO's documented access path, outside the DI graph.
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayData);
        }

        private void StartHosting(string joinCode)
        {
            // Disconnect may have been called mid-allocation — a cancelled session must not restart.
            if (_connectionState.Value != NetworkConnectionState.Connecting) return;

            SubscribeConnectionCallbacks();

            if (!NetworkManager.Singleton.StartHost())
            {
                Fail("StartHost returned false");
                return;
            }

            _joinCode.Value        = joinCode;
            _connectionState.Value = NetworkConnectionState.Hosting;
        }

        // Joined is set by OnClientConnected — StartClient only begins the handshake.
        private void StartJoining()
        {
            if (_connectionState.Value != NetworkConnectionState.Connecting) return;

            SubscribeConnectionCallbacks();

            if (!NetworkManager.Singleton.StartClient())
                Fail("StartClient returned false");
        }

        private void SubscribeConnectionCallbacks()
        {
            if (_isSubscribed) return;

            NetworkManager.Singleton.OnClientConnectedCallback  += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            _isSubscribed = true;
        }

        private void UnsubscribeConnectionCallbacks()
        {
            if (_isSubscribed && NetworkManager.Singleton)
            {
                NetworkManager.Singleton.OnClientConnectedCallback  -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }

            _isSubscribed = false;
        }

        private void OnClientConnected(ulong clientId)
        {
            NetworkManager networkManager = NetworkManager.Singleton;

            if (networkManager.IsHost)
            {
                _isSessionReady.Value = networkManager.ConnectedClients.Count >= NetworkConstants.k_SessionPlayerCount;

                // Game start is session policy, not a UI action — the host drives NGO scene sync
                // the moment the room fills; the connected client is carried along automatically.
                if (_isSessionReady.Value)
                    LoadGameScene();

                return;
            }

            // A pure client cannot enumerate peers — in a 2-player session, reaching the host means both are present.
            if (clientId == networkManager.LocalClientId)
            {
                _connectionState.Value = NetworkConnectionState.Joined;
                _isSessionReady.Value  = true;
            }
        }

        // Any disconnect kills the session — a 2-player co-op run cannot continue one-sided,
        // so the survivor's room is destroyed and a new one must be created.
        private void OnClientDisconnected(ulong clientId)
        {
            bool wasConnecting = _connectionState.Value == NetworkConnectionState.Connecting;
            ResetSession(wasConnecting ? NetworkConnectionState.Failed : NetworkConnectionState.Offline);
        }

        private void LoadGameScene()
        {
            if (_connectionState.Value != NetworkConnectionState.Hosting) return;

            NetworkManager.Singleton.SceneManager.LoadScene(SceneConstants.k_GameSceneName, LoadSceneMode.Single);
        }

        private void Fail(string reason)
        {
            Debug.LogError($"[NetworkService] Connection failed: {reason}");
            ResetSession(NetworkConnectionState.Failed);
        }

        private void ResetSession(NetworkConnectionState finalState)
        {
            UnsubscribeConnectionCallbacks();

            if (NetworkManager.Singleton && NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.Shutdown();

            _joinCode.Value        = string.Empty;
            _isSessionReady.Value  = false;
            _connectionState.Value = finalState;
        }
    }
}
