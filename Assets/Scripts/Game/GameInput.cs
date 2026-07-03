using System;

using UnityEngine;

using R3;
using Unity.Netcode;
using VContainer.Unity;

using SplitRun.Constants;
using SplitRun.Utility;

namespace SplitRun.Game
{
    public class GameInput : IStartable, ITickable, IDisposable
    {
        private enum InputRole
        {
            All,
            LaneOnly,
            VerticalOnly,
        }

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
            InputRole role = ResolveRole();

            switch (direction)
            {
                case SwipeDirection.Up:
                    if (role != InputRole.LaneOnly) _gameService.RequestJump();
                    break;
                case SwipeDirection.Down:
                    if (role != InputRole.LaneOnly) _gameService.RequestSlide();
                    break;
                default:
                    if (role != InputRole.VerticalOnly) TryChangeLane(direction);
                    break;
            }
        }

        // In a full 2-player session P1 (host) owns lanes and P2 (client) owns jump/slide;
        // solo keeps every axis. Skill double-tap stays open to both roles.
        private static InputRole ResolveRole()
        {
            NetworkManager networkManager = NetworkManager.Singleton;

            if (!networkManager || !networkManager.IsListening) return InputRole.All;
            if (!networkManager.IsHost) return InputRole.VerticalOnly;

            return networkManager.ConnectedClients.Count >= NetworkConstants.k_SessionPlayerCount
                ? InputRole.LaneOnly
                : InputRole.All;
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
