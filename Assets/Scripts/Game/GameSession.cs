using System;
using System.Threading;

using UnityEngine;

using Cysharp.Threading.Tasks;
using R3;
using Unity.Netcode;

using SplitRun.Constants;
using SplitRun.Item;

namespace SplitRun.Game
{
    // In-scene placed NetworkObject owning run-level network state: pause flow,
    // the shared track seed, and confirmed item collection. Character state stays on ServerCharacter.
    public class GameSession : NetworkBehaviour
    {
        // Declared before _pauseState so its delta applies first — views read the pauser on the state change.
        private readonly NetworkVariable<ulong>      _pausedBy   = new NetworkVariable<ulong>(0);
        private readonly NetworkVariable<PauseState> _pauseState = new NetworkVariable<PauseState>(PauseState.None);

        // 0 means unassigned — consumers wait for a non-zero seed before deriving the track layout.
        private readonly NetworkVariable<int> _runSeed = new NetworkVariable<int>(0);

        private readonly ReactiveProperty<PauseState> _pauseStateReactive = new ReactiveProperty<PauseState>(PauseState.None);
        private readonly ReactiveProperty<int>        _runSeedReactive    = new ReactiveProperty<int>(0);

        public ReadOnlyReactiveProperty<PauseState> PauseStateReactive => _pauseStateReactive;
        public ReadOnlyReactiveProperty<int>        RunSeed            => _runSeedReactive;

        // Read only synchronously on the Paused state change — no reactive mirror needed.
        public ulong PausedBy => _pausedBy.Value;

        public override void OnNetworkSpawn()
        {
            // Written during server spawn so the value ships to clients inside the spawn payload.
            if (IsServer)
                _runSeed.Value = UnityEngine.Random.Range(1, int.MaxValue);

            _pauseState.OnValueChanged += OnPauseStateChanged;
            _runSeed.OnValueChanged    += OnRunSeedChanged;

            // OnValueChanged does not fire for the initial value — manual sync required.
            _pauseStateReactive.Value = _pauseState.Value;
            _runSeedReactive.Value    = _runSeed.Value;
        }

        public override void OnNetworkDespawn()
        {
            _pauseState.OnValueChanged -= OnPauseStateChanged;
            _runSeed.OnValueChanged    -= OnRunSeedChanged;

            // An in-scene object survives despawn — reset so a dead session halts the spawner and hides overlays.
            _pauseStateReactive.Value = PauseState.None;
            _runSeedReactive.Value    = 0;
        }

        public override void OnDestroy()
        {
            _pauseStateReactive.Dispose();
            _runSeedReactive.Dispose();

            base.OnDestroy();
        }

        /// <summary>Requests a run pause. The requesting client becomes the only one allowed to resume.</summary>
        public void RequestPause()
        {
            if (!IsSpawned)
            {
                Debug.LogWarning("[GameSession] Pause requested before network spawn — ignored.");
                return;
            }

            PauseServerRpc();
        }

        /// <summary>Requests a resume. The server ignores it unless it comes from the client that paused.</summary>
        public void RequestResume()
        {
            if (!IsSpawned)
            {
                Debug.LogWarning("[GameSession] Resume requested before network spawn — ignored.");
                return;
            }

            ResumeServerRpc();
        }

        /// <summary>Server-only. Broadcasts a confirmed pickup collection to every client.</summary>
        public void ConfirmItemCollected(int spawnId)
        {
            if (!IsServer) return;
            CollectItemClientRpc(spawnId);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void PauseServerRpc(RpcParams rpcParams = default)
        {
            if (_pauseState.Value != PauseState.None) return;

            Debug.Log($"[GameSession] Paused by client {rpcParams.Receive.SenderClientId}.");
            _pausedBy.Value   = rpcParams.Receive.SenderClientId;
            _pauseState.Value = PauseState.Paused;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void ResumeServerRpc(RpcParams rpcParams = default)
        {
            if (_pauseState.Value != PauseState.Paused) return;

            if (rpcParams.Receive.SenderClientId != _pausedBy.Value)
            {
                Debug.LogWarning($"[GameSession] Resume denied — client {rpcParams.Receive.SenderClientId} is not the pauser.");
                return;
            }

            _pauseState.Value = PauseState.Countdown;
            ResumeCountdownAsync(destroyCancellationToken).Forget();
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void CollectItemClientRpc(int spawnId) => ItemEvents.NotifyCollectionConfirmed(spawnId);

        private async UniTaskVoid ResumeCountdownAsync(CancellationToken ct)
        {
            try
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(GameConstants.k_ResumeCountdownSeconds), cancellationToken: ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (_pauseState.Value != PauseState.Countdown) return;

            _pauseState.Value = PauseState.None;
        }

        private void OnPauseStateChanged(PauseState prev, PauseState next) => _pauseStateReactive.Value = next;
        private void OnRunSeedChanged(int prev, int next)                  => _runSeedReactive.Value = next;
    }
}
