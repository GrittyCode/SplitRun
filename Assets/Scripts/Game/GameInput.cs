using System;

using UnityEngine;

using R3;
using VContainer.Unity;

using SplitRun.Constants;
using SplitRun.Utility;

namespace SplitRun.Game
{
    // Lane input is gated by a cooldown timer matching the animation duration —
    // a new lane change cannot be requested until the previous animation completes.
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
            BindLaneInput();
            BindJumpSlideInput();
            BindSkillInput();
        }

        public void Tick()
        {
            if (_laneInputCooldown > 0f)
                _laneInputCooldown -= Time.deltaTime;
        }

        public void Dispose() => _disposables.Dispose();

        private void BindLaneInput()
        {
            _swipeDetector.OnSwipe
                .Where(_ => IsRunning())
                .Where(IsLaneSwipe)
                .Where(_ => _laneInputCooldown <= 0f)
                .Subscribe(dir =>
                {
                    _gameService.RequestLaneChange(ToLaneDirection(dir));
                    _laneInputCooldown = GameConstants.k_LaneMoveDuration;
                })
                .AddTo(ref _disposables);
        }

        private void BindJumpSlideInput()
        {
            _swipeDetector.OnSwipe
                .Where(_ => IsRunning())
                .Where(dir => dir == SwipeDirection.Up)
                .Subscribe(_ => _gameService.RequestJump())
                .AddTo(ref _disposables);

            _swipeDetector.OnSwipe
                .Where(_ => IsRunning())
                .Where(dir => dir == SwipeDirection.Down)
                .Subscribe(_ => _gameService.RequestSlide())
                .AddTo(ref _disposables);
        }

        private void BindSkillInput()
        {
            _swipeDetector.OnDoubleTap
                .Where(_ => IsRunning())
                .Subscribe(_ => _gameService.RequestSkill())
                .AddTo(ref _disposables);
        }

        private bool IsRunning() => _gameService.Phase.CurrentValue == GamePhase.Running;

        private static bool IsLaneSwipe(SwipeDirection dir)
            => dir is SwipeDirection.Left or SwipeDirection.Right;

        private static int ToLaneDirection(SwipeDirection dir)
            => dir == SwipeDirection.Left ? -1 : 1;
    }
}
