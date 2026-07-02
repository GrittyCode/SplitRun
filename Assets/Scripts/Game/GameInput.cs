using System;

using UnityEngine;

using R3;
using VContainer.Unity;

using SplitRun.Constants;
using SplitRun.Utility;

namespace SplitRun.Game
{
    public class GameInput : IStartable, ITickable, IDisposable
    {
        private readonly SwipeDetector _swipeDetector;
        private readonly GameService   _gameService;

        private float         _laneInputCooldown;
        private DisposableBag _disposables;

        public GameInput(SwipeDetector swipeDetector, GameService gameService)
        {
            _swipeDetector = swipeDetector;
            _gameService   = gameService;
        }

        public void Start()
        {
            _swipeDetector.OnSwipe
                .Where(_ => IsRunning())
                .Subscribe(OnSwipe)
                .AddTo(ref _disposables);

            _swipeDetector.OnDoubleTap
                .Where(_ => IsRunning())
                .Subscribe(_ => _gameService.RequestSkill())
                .AddTo(ref _disposables);
        }

        public void Tick()
        {
            if (_laneInputCooldown > 0f)
                _laneInputCooldown -= Time.deltaTime;
        }

        public void Dispose() => _disposables.Dispose();

        private void OnSwipe(SwipeDirection direction)
        {
            switch (direction)
            {
                case SwipeDirection.Up:
                    _gameService.RequestJump();
                    break;
                case SwipeDirection.Down:
                    _gameService.RequestSlide();
                    break;
                default:
                    TryChangeLane(direction);
                    break;
            }
        }

        // Gated by a cooldown matching the lane tween so a new change can't start mid-animation.
        private void TryChangeLane(SwipeDirection direction)
        {
            if (_laneInputCooldown > 0f) return;

            _gameService.RequestLaneChange(direction == SwipeDirection.Left ? -1 : 1);
            _laneInputCooldown = GameConstants.k_LaneMoveDuration;
        }

        private bool IsRunning() => _gameService.Phase.CurrentValue == GamePhase.Running;
    }
}
