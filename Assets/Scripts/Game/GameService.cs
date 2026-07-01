using System;

using R3;
using VContainer.Unity;

using SplitRun.Character;
using SplitRun.Constants;

namespace SplitRun.Game
{
    public class GameService : IStartable, IDisposable
    {
        private ICharacter    _character;
        private DisposableBag _characterDisposables;
        private DisposableBag _disposables;

        private readonly ReactiveProperty<GamePhase>  _phase            = new ReactiveProperty<GamePhase>(GamePhase.Lobby);
        private readonly ReactiveProperty<float>      _currentDistance  = new ReactiveProperty<float>(0f);
        private readonly ReactiveProperty<int>        _currentHp        = new ReactiveProperty<int>(GameConstants.k_MaxHp);
        private readonly ReactiveProperty<float>      _speed            = new ReactiveProperty<float>(GameConstants.k_BaseRunSpeed);
        private readonly ReactiveProperty<SkillState> _skillState       = new ReactiveProperty<SkillState>(SkillState.Ready);
        private readonly ReactiveProperty<SkillType>  _activeSkill      = new ReactiveProperty<SkillType>(SkillType.None);
        private readonly Subject<int>                 _onZoneEntered    = new Subject<int>();

        public ReadOnlyReactiveProperty<GamePhase>  Phase             => _phase;
        public ReadOnlyReactiveProperty<float>      CurrentDistance   => _currentDistance;
        public ReadOnlyReactiveProperty<int>        CurrentHp         => _currentHp;
        public ReadOnlyReactiveProperty<float>      Speed             => _speed;
        public ReadOnlyReactiveProperty<SkillState> CurrentSkillState => _skillState;
        public ReadOnlyReactiveProperty<SkillType>  ActiveSkill       => _activeSkill;
        public Observable<int>                      OnZoneEntered     => _onZoneEntered;

        public void Start()
        {
            CharacterEvents.OnSpawned   += OnCharacterSpawned;
            CharacterEvents.OnDespawned += OnCharacterDespawned;

            _currentDistance.Value = 0f;
            _currentHp.Value       = GameConstants.k_MaxHp;
            _speed.Value           = GameConstants.k_BaseRunSpeed;
            _phase.Value           = GamePhase.Lobby;
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
            _skillState.Dispose();
            _activeSkill.Dispose();
            _onZoneEntered.Dispose();
        }

        /// <summary>Transitions phase to Running. Called once both players confirm ready.</summary>
        public void StartRun()
        {
            _phase.Value = GamePhase.Running;
            _character?.SetRunning(true);
        }

        /// <summary>Transitions phase to GameOver and locks in the final distance.</summary>
        public void EndRun(float finalDistance)
        {
            _currentDistance.Value = finalDistance;
            _phase.Value           = GamePhase.GameOver;
            _character?.SetRunning(false);

            // TODO(data): inject PlayerDataService and call UpdateBestDistance((int)finalDistance)
        }

        public void RequestLaneChange(int direction) => _character?.RequestLaneChange(direction);
        public void RequestJump()                    => _character?.RequestJump();
        public void RequestSlide()                   => _character?.RequestSlide();
        public void RequestSkill()                   => _character?.ActivateSkill();

        // The character is spawned at runtime outside the DI graph, so GameService mirrors its
        // reactives here to give injected views a stable seam across spawn/despawn.
        private void OnCharacterSpawned(ICharacter character)
        {
            _character         = character;
            _activeSkill.Value = character.ActiveSkill;
            character.SetRunning(_phase.Value == GamePhase.Running);

            character.HpReactive
                .Subscribe(hp => _currentHp.Value = hp)
                .AddTo(ref _characterDisposables);

            character.DistanceReactive
                .Subscribe(distance => _currentDistance.Value = distance)
                .AddTo(ref _characterDisposables);

            character.SpeedReactive
                .Subscribe(speed => _speed.Value = speed)
                .AddTo(ref _characterDisposables);

            character.SkillStateReactive
                .Subscribe(state => _skillState.Value = state)
                .AddTo(ref _characterDisposables);

            // ReactiveProperty skips re-emission when value is unchanged — EndRun never fires twice.
            character.HpReactive
                .Where(hp => hp <= 0 && _phase.Value == GamePhase.Running)
                .Subscribe(_ => EndRun(_currentDistance.Value))
                .AddTo(ref _characterDisposables);
        }

        private void OnCharacterDespawned(ICharacter character)
        {
            if (_character != character) return;

            _character         = null;
            _activeSkill.Value = SkillType.None;
            _skillState.Value  = SkillState.Ready;
            _characterDisposables.Dispose();
        }
    }
}
