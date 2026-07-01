using UnityEngine;

using SplitRun.Item;
using SplitRun.Obstacle;

namespace SplitRun.Character
{
    [RequireComponent(typeof(Collider))]
    public class CollisionReporter : MonoBehaviour
    {
        private ICharacter _character;

        private void Start()
        {
            // The character is spawned at runtime outside the DI container, so its owner is
            // resolved from the hierarchy rather than injected.
            _character = GetComponentInParent<ICharacter>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out TrackObstacle obstacle))
            {
                obstacle.Impacted();
                _character.ReportCollision();
                return;
            }

            if (other.TryGetComponent(out ItemPickup item))
                ItemEvents.NotifyCollected(item);
        }
    }
}
