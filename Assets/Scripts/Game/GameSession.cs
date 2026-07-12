using System;
using System.Collections.Generic;
using System.Threading;

using UnityEngine;

using Cysharp.Threading.Tasks;
using R3;
using Unity.Netcode;

using SplitRun.Constants;

namespace SplitRun.Game
{
    public enum PauseState
    {
        None      = 0,
        Paused    = 1,
        Countdown = 2,
    }

    public enum RunStartState
    {
        AwaitingPlayers = 0,
        Intro           = 1,
        Live            = 2,
    }

    public class GameSession : NetworkBehaviour
    {
        private readonly NetworkVariable<RunStartState> _runStartState = new NetworkVariable<RunStartState>(RunStartState.AwaitingPlayers);

        // Declared before _pauseState so its delta applies first — views read the pauser on the state change.
        private readonly NetworkVariable<ulong>      _pausedBy   = new NetworkVariable<ulong>(0);
        private readonly NetworkVariable<PauseState> _pauseState = new NetworkVariable<PauseState>(PauseState.None);

        // 0 means unassigned — consumers wait for a non-zero seed before deriving the track layout.
        private readonly NetworkVariable<int> _runSeed = new NetworkVariable<int>(0);

        private readonly ReactiveProperty<RunStartState> _runStartReactive   = new ReactiveProperty<RunStartState>(RunStartState.AwaitingPlayers);
        private readonly ReactiveProperty<PauseState>    _pauseStateReactive = new ReactiveProperty<PauseState>(PauseState.None);
        private readonly ReactiveProperty<int>           _runSeedReactive    = new ReactiveProperty<int>(0);

        private readonly Subject<int> _onItemCollectionConfirmed = new Subject<int>();

        private readonly HashSet<ulong> _readyClients = new HashSet<ulong>();

        public ReadOnlyReactiveProperty<RunStartState> RunStartReactive   => _runStartReactive;
        public ReadOnlyReactiveProperty<PauseState>    PauseStateReactive => _pauseStateReactive;
        public ReadOnlyReactiveProperty<int>           RunSeed            => _runSeedReactive;

        public Observable<int> OnItemCollectionConfirmed => _onItemCollectionConfirmed;

        // Read only synchronously on the Paused state change — no reactive mirror needed.
        public ulong PausedBy => _pausedBy.Value;

        public override void OnNetworkSpawn()
        {
            // Written during server spawn so the value ships to clients inside the spawn payload.
            if (IsServer)
                _runSeed.Value = UnityEngine.Random.Range(1, int.MaxValue);

            _runStartState.OnValueChanged += OnRunStartStateChanged;
            _pauseState.OnValueChanged    += OnPauseStateChanged;
            _runSeed.OnValueChanged       += OnRunSeedChanged;

            // OnValueChanged does not fire for the initial value — manual sync required.
            _runStartReactive.Value   = _runStartState.Value;
            _pauseStateReactive.Value = _pauseState.Value;
            _runSeedReactive.Value    = _runSeed.Value;

            // Reporting readiness here means the local peer already has the game scene synced.
            SignalReadyServerRpc();
        }

        public override void OnNetworkDespawn()
        {
            _runStartState.OnValueChanged -= OnRunStartStateChanged;
            _pauseState.OnValueChanged    -= OnPauseStateChanged;
            _runSeed.OnValueChanged       -= OnRunSeedChanged;

            _readyClients.Clear();

            // An in-scene object survives despawn — reset so a dead session halts the spawner.
            _runStartReactive.Value   = RunStartState.AwaitingPlayers;
            _pauseStateReactive.Value = PauseState.None;
            _runSeedReactive.Value    = 0;
        }

        public override void OnDestroy()
        {
            _runStartReactive.Dispose();
            _pauseStateReactive.Dispose();
            _runSeedReactive.Dispose();
            _onItemCollectionConfirmed.Dispose();

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
        private void SignalReadyServerRpc(RpcParams rpcParams = default)
        {
            if (_runStartState.Value != RunStartState.AwaitingPlayers) return;

            _readyClients.Add(rpcParams.Receive.SenderClientId);
            if (_readyClients.Count < NetworkManager.ConnectedClientsIds.Count) return;

            // Solo has no role split to explain — start immediately; a 2-player run shows the intro first.
            if (NetworkManager.ConnectedClientsIds.Count <= 1)
            {
                _runStartState.Value = RunStartState.Live;
                return;
            }

            _runStartState.Value = RunStartState.Intro;
            IntroCountdownAsync(destroyCancellationToken).Forget();
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
        private void CollectItemClientRpc(int spawnId) => _onItemCollectionConfirmed.OnNext(spawnId);

        private async UniTaskVoid IntroCountdownAsync(CancellationToken ct)
        {
            try
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(GameConstants.k_RunIntroSeconds), cancellationToken: ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            // An in-scene object can despawn without being destroyed mid-timer — never write a NetworkVariable then.
            if (!IsSpawned || _runStartState.Value != RunStartState.Intro) return;

            _runStartState.Value = RunStartState.Live;
        }

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

            // An in-scene object can despawn without being destroyed mid-timer — never write a NetworkVariable then.
            if (!IsSpawned || _pauseState.Value != PauseState.Countdown) return;

            _pauseState.Value = PauseState.None;
        }

        private void OnRunStartStateChanged(RunStartState prev, RunStartState next) => _runStartReactive.Value = next;
        private void OnPauseStateChanged(PauseState prev, PauseState next)          => _pauseStateReactive.Value = next;
        private void OnRunSeedChanged(int prev, int next)                           => _runSeedReactive.Value = next;
    }
}
