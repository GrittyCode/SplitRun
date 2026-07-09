using System;

using R3;
using VContainer.Unity;

using SplitRun.Character;
using SplitRun.Constants;
using SplitRun.Data;
using SplitRun.Mission;

namespace SplitRun.Game
{
    public class GameService : IStartable, IDisposable
    {
        private readonly GameSession       _gameSession;
        private readonly PlayerDataService _playerDataService;
        private readonly MissionService    _missionService;

        private ICharacter    _character;
        private DisposableBag _characterDisposables;
        private DisposableBag _disposables;

        private int _runJumps;
        private int _runSlides;
        private int _runLaneChanges;

        private bool _isNewBestDistance;

        private readonly ReactiveProperty<GamePhase>  _phase           = new ReactiveProperty<GamePhase>(GamePhase.Lobby);
        private readonly ReactiveProperty<float>      _currentDistance = new ReactiveProperty<float>(0f);
        private readonly ReactiveProperty<int>        _currentHp       = new ReactiveProperty<int>(GameConstants.k_MaxHp);
        private readonly ReactiveProperty<SkillState> _skillState      = new ReactiveProperty<SkillState>(SkillState.Ready);
        private readonly ReactiveProperty<SkillType>  _activeSkill     = new ReactiveProperty<SkillType>(SkillType.None);

        private readonly Subject<Unit> _endSessionRequested = new Subject<Unit>();

        public GameService(GameSession gameSession, PlayerDataService playerDataService, MissionService missionService)
        {
            _gameSession       = gameSession;
            _playerDataService = playerDataService;
            _missionService    = missionService;
        }

        public ReadOnlyReactiveProperty<GamePhase>  Phase             => _phase;
        public ReadOnlyReactiveProperty<float>      CurrentDistance   => _currentDistance;
        public ReadOnlyReactiveProperty<int>        CurrentHp         => _currentHp;
        public ReadOnlyReactiveProperty<SkillState> CurrentSkillState => _skillState;
        public ReadOnlyReactiveProperty<SkillType>  ActiveSkill       => _activeSkill;

        // Set at EndRun, read by the result overlay — whether this run beat the stored record.
        public bool IsNewBestDistance => _isNewBestDistance;

        // Raised by the result overlay's quit button; GameEntryPoint owns the actual teardown.
        public Observable<Unit> EndSessionRequested => _endSessionRequested;

        public void Start()
        {
            CharacterEvents.OnSpawned   += OnCharacterSpawned;
            CharacterEvents.OnDespawned += OnCharacterDespawned;

            _currentDistance.Value = 0f;
            _currentHp.Value       = GameConstants.k_MaxHp;
            _phase.Value           = GamePhase.Lobby;

            _gameSession.RunStartReactive
                .Subscribe(OnRunStartStateChanged)
                .AddTo(ref _disposables);

            _gameSession.PauseStateReactive
                .Subscribe(OnPauseStateChanged)
                .AddTo(ref _disposables);
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
            _skillState.Dispose();
            _activeSkill.Dispose();

            _endSessionRequested.Dispose();
        }

        /// <summary>Transitions phase to Running. Called on the Live signal once both players are ready.</summary>
        public void StartRun()
        {
            _phase.Value = GamePhase.Running;
            _character?.SetRunning(true);
        }

        /// <summary>Transitions phase to GameOver. CurrentDistance already holds the final value.</summary>
        public void EndRun()
        {
            int finalDistance = (int)_currentDistance.Value;

            // Captured before the write — UpdateBestDistance overwrites the prior record in place.
            _isNewBestDistance = finalDistance > _playerDataService.BestDistance.CurrentValue;

            _playerDataService.UpdateBestDistance(finalDistance);
            _missionService.ReportRun(finalDistance, _runJumps, _runSlides, _runLaneChanges);

            _phase.Value = GamePhase.GameOver;
            _character?.SetRunning(false);
        }

        /// <summary>Requests a pause. The server records the requester as the only one allowed to resume.</summary>
        public void RequestPause()
        {
            if (_phase.Value != GamePhase.Running) return;
            _gameSession.RequestPause();
        }

        /// <summary>Ends the current session from the result overlay — routed to GameEntryPoint for teardown.</summary>
        public void RequestEndSession() => _endSessionRequested.OnNext(Unit.Default);

        public void RequestLaneChange(int direction) => _character?.RequestLaneChange(direction);
        public void RequestJump()                    => _character?.RequestJump();
        public void RequestSlide()                   => _character?.RequestSlide();
        public void RequestSkill()                   => _character?.ActivateSkill();

        // Mirrors the character's reactives so injected views keep a stable seam across spawn/despawn.
        private void OnCharacterSpawned(ICharacter character)
        {
            _character         = character;
            _activeSkill.Value = character.ActiveSkill;
            character.SetRunning(_phase.Value == GamePhase.Running && _gameSession.PauseStateReactive.CurrentValue == PauseState.None);

            // A fresh character marks a fresh run — action tallies restart from zero.
            _runJumps       = 0;
            _runSlides      = 0;
            _runLaneChanges = 0;

            // ReactiveProperty skips re-emission when value is unchanged — EndRun never fires twice.
            character.HpReactive
                .Subscribe(hp =>
                {
                    _currentHp.Value = hp;
                    if (hp <= 0 && _phase.Value == GamePhase.Running) EndRun();
                })
                .AddTo(ref _characterDisposables);

            character.DistanceReactive
                .Subscribe(distance => _currentDistance.Value = distance)
                .AddTo(ref _characterDisposables);

            character.SkillStateReactive
                .Subscribe(state => _skillState.Value = state)
                .AddTo(ref _characterDisposables);

            character.VerticalStateReactive
                .Subscribe(CountVerticalAction)
                .AddTo(ref _characterDisposables);

            // Skip(1) drops the initial lane emitted on subscribe so only real changes count.
            character.LaneReactive
                .Skip(1)
                .Subscribe(_ => _runLaneChanges++)
                .AddTo(ref _characterDisposables);
        }

        private void OnCharacterDespawned(ICharacter character)
        {
            if (_character != character) return;

            _character         = null;
            _activeSkill.Value = SkillType.None;
            _skillState.Value  = SkillState.Ready;

            _characterDisposables.Dispose();

            // A disposed bag disposes anything added later — reset it for the next spawn.
            _characterDisposables = new DisposableBag();
        }

        private void CountVerticalAction(VerticalState state)
        {
            switch (state)
            {
                case VerticalState.Jumping:
                    _runJumps++;
                    break;
                case VerticalState.Sliding:
                    _runSlides++;
                    break;
            }
        }

        private void OnRunStartStateChanged(RunStartState state)
        {
            switch (state)
            {
                case RunStartState.Intro:
                    _phase.Value = GamePhase.Intro;
                    break;
                case RunStartState.Live:
                    StartRun();
                    break;
            }
        }

        private void OnPauseStateChanged(PauseState state)
        {
            if (state != PauseState.None && _phase.Value == GamePhase.Running)
            {
                _phase.Value = GamePhase.Paused;
                _character?.SetRunning(false);
                return;
            }

            if (state == PauseState.None && _phase.Value == GamePhase.Paused)
            {
                _phase.Value = GamePhase.Running;
                _character?.SetRunning(true);
            }
        }
    }
}
