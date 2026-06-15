using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

using R3;

using SplitRun.Constants;

using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace SplitRun.Utility
{
    public class SwipeDetector : MonoBehaviour
    {
        private readonly Subject<SwipeDirection> _onSwipe = new Subject<SwipeDirection>();
        public Observable<SwipeDirection> OnSwipe => _onSwipe;

        private Vector2 _touchStartPosition;

        private void OnEnable()
        {
            // TouchSimulation maps mouse input to touch events in the Unity Editor
#if UNITY_EDITOR
            TouchSimulation.Enable();
#endif
            EnhancedTouchSupport.Enable();
            Touch.onFingerDown += OnFingerDown;
            Touch.onFingerUp   += OnFingerUp;
        }

        private void OnDisable()
        {
            Touch.onFingerDown -= OnFingerDown;
            Touch.onFingerUp   -= OnFingerUp;
            EnhancedTouchSupport.Disable();
#if UNITY_EDITOR
            TouchSimulation.Disable();
#endif
        }

        private void OnDestroy()
        {
            _onSwipe.Dispose();
        }

        private void OnFingerDown(Finger finger)
        {
            _touchStartPosition = finger.currentTouch.screenPosition;
        }

        private void OnFingerUp(Finger finger)
        {
            Vector2 delta = finger.currentTouch.screenPosition - _touchStartPosition;

            if (delta.magnitude < GameConstants.k_SwipeMinDistancePx)
                return;

            SwipeDirection direction = ResolveDirection(delta);
            _onSwipe.OnNext(direction);
        }

        private static SwipeDirection ResolveDirection(Vector2 delta)
        {
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
                return delta.x > 0 ? SwipeDirection.Right : SwipeDirection.Left;

            return delta.y > 0 ? SwipeDirection.Up : SwipeDirection.Down;
        }
    }
}
