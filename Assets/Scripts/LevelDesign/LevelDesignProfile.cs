using UnityEngine;

namespace SplitRun.LevelDesign
{
    [CreateAssetMenu(fileName = "LDP_New", menuName = "SplitRun/Level Design Profile")]
    public class LevelDesignProfile : ScriptableObject
    {
        [SerializeField] private string         _profileName;
        [SerializeField] private ObstacleBand[] _obstacleBands;
        [SerializeField] private float          _coinSpawnMultiplier = 1f;

        public string ProfileName         => _profileName;
        public float  CoinSpawnMultiplier => _coinSpawnMultiplier;
        public bool   HasBands            => _obstacleBands != null && _obstacleBands.Length > 0;

        /// <summary>Returns the band with the largest StartDistance not exceeding the given distance.</summary>
        public ObstacleBand ResolveBand(float distance)
        {
            ObstacleBand active = _obstacleBands[0];

            for (int i = 0; i < _obstacleBands.Length; i++)
            {
                if (_obstacleBands[i].StartDistance > distance) break;

                active = _obstacleBands[i];
            }

            return active;
        }

        private void OnValidate()
        {
            if (_obstacleBands == null) return;

            for (int i = 1; i < _obstacleBands.Length; i++)
            {
                if (_obstacleBands[i].StartDistance >= _obstacleBands[i - 1].StartDistance) continue;

                Debug.LogWarning(
                    $"[LevelDesignProfile] '{name}' bands are not in ascending StartDistance order.", this);
                return;
            }
        }
    }
}
