using UnityEngine;

namespace SplitRun.Obstacle
{
     public class Obstacle : MonoBehaviour
    {
        [SerializeField] private ObstacleType _obstacleType;

        public ObstacleType ObstacleType => _obstacleType;
    }
}
