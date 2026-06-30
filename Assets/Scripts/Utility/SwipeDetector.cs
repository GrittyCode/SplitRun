using System;

using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

using R3;
using VContainer.Unity;

using SplitRun.Constants;

using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace SplitRun.Utility
{
    // Registered as an entry point in GameLifetimeScope — requires no scene GameObject.
    public class SwipeDetector : IStartable, IDisposable
    {
        private readonly Subject<SwipeDirection> _onSwipe     = new Subject<SwipeDirection>();
        private readonly Subject<Unit>           _onDoubleTap = new Subject<Unit>();

        public Observable<SwipeDirection> OnSwipe     => _onSwipe;
        public Observable<Unit>           OnDoubleTap => _onDoubleTap;

        private Vector2 _touchStartPosition;
        private float   _lastTapTime = float.NegativeInfinity;

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
            _onSwipe.Dispose();
            _onDoubleTap.Dispose();
        }

        private void OnFingerDown(Finger finger)
        {
            _touchStartPosition = finger.currentTouch.screenPosition;
        }

        private void OnFingerUp(Finger finger)
        {
            Vector2 delta = finger.currentTouch.screenPosition - _touchStartPosition;

            if (delta.magnitude < GameConstants.k_SwipeMinDistancePx)
            {
                DetectDoubleTap();
                return;
            }

            _onSwipe.OnNext(ResolveDirection(delta));
        }

        private void DetectDoubleTap()
        {
            float now = Time.time;

            if (now - _lastTapTime <= GameConstants.k_DoubleTapWindow)
            {
                _onDoubleTap.OnNext(Unit.Default);
                _lastTapTime = float.NegativeInfinity;
                return;
            }

            _lastTapTime = now;
        }

        private static SwipeDirection ResolveDirection(Vector2 delta)
        {
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
                return delta.x > 0 ? SwipeDirection.Right : SwipeDirection.Left;

            return delta.y > 0 ? SwipeDirection.Up : SwipeDirection.Down;
        }
    }
}
