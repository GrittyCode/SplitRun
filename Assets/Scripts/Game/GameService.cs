using System;

using UnityEngine;

using R3;
using VContainer.Unity;

using SplitRun.Character;
using SplitRun.Constants;

namespace SplitRun.Game
{
    // Tracks the active ICharacter via CharacterEvents and exposes action requests
    // so other systems never reference ServerCharacter or LocalCharacter directly.
    public class GameService : IStartable, IDisposable
    {
        private ICharacter    _character;
        private DisposableBag _characterDisposables;
        private DisposableBag _disposables;

        private readonly ReactiveProperty<GamePhase> _phase           = new ReactiveProperty<GamePhase>(GamePhase.Lobby);
        private readonly ReactiveProperty<float>     _currentDistance = new ReactiveProperty<float>(0f);
        private readonly ReactiveProperty<int>       _currentHp       = new ReactiveProperty<int>(GameConstants.k_MaxHp);
        private readonly ReactiveProperty<float>     _speed           = new ReactiveProperty<float>(GameConstants.k_BaseRunSpeed);

        private readonly Subject<int> _onZoneEntered = new Subject<int>();

        public ReadOnlyReactiveProperty<GamePhase> Phase           => _phase;
        public ReadOnlyReactiveProperty<float>     CurrentDistance => _currentDistance;
        public ReadOnlyReactiveProperty<int>       CurrentHp       => _currentHp;
        public ReadOnlyReactiveProperty<float>     Speed           => _speed;
        public Observable<int>                     OnZoneEntered   => _onZoneEntered;

        public void Start()
        {
            CharacterEvents.OnSpawned   += OnCharacterSpawned;
            CharacterEvents.OnDespawned += OnCharacterDespawned;

            _currentDistance.Value = 0f;
            _currentHp.Value       = GameConstants.k_MaxHp;
            _speed.Value           = GameConstants.k_BaseRunSpeed;
            _phase.Value           = GamePhase.Lobby;

            Debug.Log("[GameService] Initialized");
        }

        public void Dispose()
        {
            CharacterEvents.OnSpawned   -= OnCharacterSpawned;
            CharacterEvents.OnDespawned -= OnCharacterDespawned;

            _characterDisposables.Dispose();
            _disposables.Dispose();

            _phase.Dispose();
            _currentDistance.Dispose();
            _currentHp.Dispose();
            _speed.Dispose();
            _onZoneEntered.Dispose();
        }

        /// <summary>Transitions phase to Running. Called once both players confirm ready.</summary>
        public void StartRun()
        {
            _phase.Value = GamePhase.Running;
            Debug.Log("[GameService] Run started");
        }

        /// <summary>Transitions phase to GameOver and locks in the final distance.</summary>
        public void EndRun(float finalDistance)
        {
            _currentDistance.Value = finalDistance;
            _phase.Value           = GamePhase.GameOver;

            // TODO(data): inject PlayerDataService and call UpdateBestDistance((int)finalDistance)
            Debug.Log($"[GameService] Run ended — distance: {finalDistance:F0}m");
        }

        // Callers (GameInput, etc.) never hold a reference to ICharacter directly.
        public void RequestLaneChange(int direction) => _character?.RequestLaneChange(direction);
        public void RequestJump()                    => _character?.RequestJump();
        public void RequestSlide()                   => _character?.RequestSlide();

        private void OnCharacterSpawned(ICharacter character)
        {
            _character = character;

            // Mirror character HP into GameService so UI and services read from one place.
            character.HpReactive
                .Subscribe(hp => _currentHp.Value = hp)
                .AddTo(ref _characterDisposables);
        }

        private void OnCharacterDespawned(ICharacter character)
        {
            if (_character != character) return;
            _character = null;
            _characterDisposables.Dispose();
        }
    }
}
