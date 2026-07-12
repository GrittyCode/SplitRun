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

        private float _lastTapTime = float.NegativeInfinity;
        private float _nextLaneInputTime;

        public GameInput(GameService gameService) => _gameService = gameService;

        public void Start()
        {
            // Desktop has no touchscreen, so EnhancedTouch stays silent unless a mouse is bridged to it.
#if UNITY_EDITOR || UNITY_STANDALONE
            TouchSimulation.Enable();
#endif
            EnhancedTouchSupport.Enable();
            Touch.onFingerUp += OnFingerUp;
        }

        public void Dispose()
        {
            Touch.onFingerUp -= OnFingerUp;
            EnhancedTouchSupport.Disable();
#if UNITY_EDITOR || UNITY_STANDALONE
            TouchSimulation.Disable();
#endif
        }

        private void OnFingerUp(Finger finger)
        {
            if (!IsRunning()) return;

            // startScreenPosition is tracked per finger, so an overlapping second touch cannot corrupt the swipe origin.
            Vector2 delta = finger.currentTouch.screenPosition - finger.currentTouch.startScreenPosition;

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
