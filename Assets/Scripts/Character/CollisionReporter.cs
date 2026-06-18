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
            if (!other.TryGetComponent<Obstacle.Obstacle>(out _)) return;

            _character.ReportCollision();
        }
    }
}
