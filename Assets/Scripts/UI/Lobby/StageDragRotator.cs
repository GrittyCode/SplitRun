using UnityEngine;
using UnityEngine.EventSystems;

namespace SplitRun.UI.Lobby
{
    // Spins the stage about Y from a pointer drag; presentation-only, never the game character.
    public class StageDragRotator : MonoBehaviour, IDragHandler
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float     _rotationSpeed = 0.2f;

        public void OnDrag(PointerEventData eventData)
        {
            if (!_target)
                return;

            _target.Rotate(0f, -eventData.delta.x * _rotationSpeed, 0f, Space.Self);
        }
    }
}
