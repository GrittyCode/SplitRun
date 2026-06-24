using UnityEngine;

using SplitRun.Obstacle;

namespace SplitRun.Character
{
    [RequireComponent(typeof(Collider))]
    public class CollisionReporter : MonoBehaviour
    {
        private ICharacter _character;

        private void Start()
        {
            _character = GetComponentInParent<ICharacter>();
        }

        // Every obstacle keeps its single BoxCollider on the root alongside TrackObstacle
        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out TrackObstacle obstacle)) return;

            obstacle.Impacted();
            _character.ReportCollision();
        }
    }
}
