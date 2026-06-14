using System;

using UnityEngine;

using R3;

using SplitRun.Constants;

namespace SplitRun.Game
{
    public class GameService : IDisposable
    {
        private readonly ReactiveProperty<GamePhase> _phase           = new ReactiveProperty<GamePhase>(GamePhase.Lobby);
        private readonly ReactiveProperty<float>     _currentDistance = new ReactiveProperty<float>(0f);
        private readonly ReactiveProperty<int>       _currentHp       = new ReactiveProperty<int>(GameConstants.k_MaxHp);
        private readonly ReactiveProperty<float>     _speed           = new ReactiveProperty<float>(GameConstants.k_BaseRunSpeed);

        private readonly Subject<int> _onZoneEntered = new Subject<int>();

        public ReadOnlyReactiveProperty<float>     CurrentDistance => _currentDistance;
        public ReadOnlyReactiveProperty<int>       CurrentHp       => _currentHp;
        public ReadOnlyReactiveProperty<GamePhase> Phase           => _phase;
        public ReadOnlyReactiveProperty<float>     Speed           => _speed;

        public Observable<int> OnZoneEntered => _onZoneEntered;

        /// <summary>Resets all runtime state to initial values. Called by GameEntryPoint on scene load.</summary>
        public void Initialize()
        {
            _currentDistance.Value = 0f;
            _currentHp.Value       = GameConstants.k_MaxHp;
            _speed.Value           = GameConstants.k_BaseRunSpeed;
            _phase.Value           = GamePhase.Lobby;
            Debug.Log("[GameService] Initialized");
        }

        /// <summary>Transitions phase to Running. Called once both players confirm ready.</summary>
        public void StartRun()
        {
            _phase.Value = GamePhase.Running;
            Debug.Log("[GameService] Run started");
        }

        /// <summary>
        /// Transitions phase to GameOver and locks in the final distance.
        /// Called when HP reaches zero (server-authoritative collision result).
        /// </summary>
        public void EndRun(float finalDistance)
        {
            _currentDistance.Value = finalDistance;
            _phase.Value           = GamePhase.GameOver;

            // TODO(data): inject PlayerDataService and call UpdateBestDistance((int)finalDistance)
            // Wire in Phase 2 when HP = 0 → EndRun path is implemented

            Debug.Log($"[GameService] Run ended — distance: {finalDistance:F0}m");
        }

        public void Dispose()
        {
            // Notifies all active ChunkSpawner / MissionService subscribers via OnCompleted
            _onZoneEntered.Dispose();
        }
    }
}
