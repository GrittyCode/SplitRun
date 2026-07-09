using System;

using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

using R3;
using VContainer.Unity;

using SplitRun.Constants;
using SplitRun.Network;

using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace SplitRun.Game
{
    public enum SwipeDirection
    {
        Left,
        Right,
        Up,
        Down,
    }

    public class GameInput : IStartable, IDisposable
    {
        private readonly GameService _gameService;

        private Vector2 _touchStartPosition;
        private float   _lastTapTime = float.NegativeInfinity;
        private float   _nextLaneInputTime;

        public GameInput(GameService gameService) => _gameService = gameService;

        public void Start()
        {
#if UNITY_EDITOR
            TouchSimulation.Enable();
#endif
            EnhancedTouchSupport.Enable();
            Touch.onFingerDown += OnFingerDown;
            Touch.onFingerUp   += OnFingerUp;
        }

        public void Dispose()
        {
            Touch.onFingerDown -= OnFingerDown;
            Touch.onFingerUp   -= OnFingerUp;
            EnhancedTouchSupport.Disable();
#if UNITY_EDITOR
            TouchSimulation.Disable();
#endif
        }

        private void OnFingerDown(Finger finger) => _touchStartPosition = finger.currentTouch.screenPosition;

        private void OnFingerUp(Finger finger)
        {
            if (!IsRunning()) return;

            Vector2 delta = finger.currentTouch.screenPosition - _touchStartPosition;

            if (delta.magnitude < GameConstants.k_SwipeMinDistancePx)
            {
                DetectDoubleTap();
                return;
            }

            OnSwipe(ResolveDirection(delta));
        }

        private void DetectDoubleTap()
        {
            float now = Time.time;

            if (now - _lastTapTime <= GameConstants.k_DoubleTapWindow)
            {
                _gameService.RequestSkill();
                _lastTapTime = float.NegativeInfinity;
                return;
            }

            _lastTapTime = now;
        }

        private void OnSwipe(SwipeDirection direction)
        {
            SessionRole role = SessionRoleResolver.Resolve();

            switch (direction)
            {
                case SwipeDirection.Up:
                    if (role != SessionRole.LaneOnly) _gameService.RequestJump();
                    break;
                case SwipeDirection.Down:
                    if (role != SessionRole.LaneOnly) _gameService.RequestSlide();
                    break;
                default:
                    if (role != SessionRole.VerticalOnly) TryChangeLane(direction);
                    break;
            }
        }

        // Gated by a cooldown matching the lane tween so a new change can't start mid-animation.
        private void TryChangeLane(SwipeDirection direction)
        {
            if (Time.time < _nextLaneInputTime) return;

            _gameService.RequestLaneChange(direction == SwipeDirection.Left ? -1 : 1);
            _nextLaneInputTime = Time.time + CharacterConstants.k_LaneMoveDuration;
        }

        private static SwipeDirection ResolveDirection(Vector2 delta)
        {
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
                return delta.x > 0 ? SwipeDirection.Right : SwipeDirection.Left;

            return delta.y > 0 ? SwipeDirection.Up : SwipeDirection.Down;
        }

        private bool IsRunning() => _gameService.Phase.CurrentValue == GamePhase.Running;
    }
}
