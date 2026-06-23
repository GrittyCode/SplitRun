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

        private void OnTriggerEnter(Collider other)
        {
            // GetComponentInParent: composite coop colliders live on child cubes, so the
            // TrackObstacle sits one level up. For single obstacles (collider on the root)
            // this returns the obstacle itself — one path covers both.
            TrackObstacle obstacle = other.GetComponentInParent<TrackObstacle>();
            if (obstacle == null) return;

            obstacle.Impacted();
            _character.ReportCollision();
        }
    }
}
