using System.Collections.Generic;

using UnityEngine;

using R3;
using VContainer;

using SplitRun.Constants;
using SplitRun.Obstacle;

namespace SplitRun.Game
{
    // MonoBehaviour placed in the Game scene and registered via GameLifetimeScope.RegisterComponent.
    // Holds one ObstaclePool per prefab variant, spawns independent obstacles at a fixed Z
    // interval ahead of the character, and returns them to their pool once they fall behind.
    public class ObstacleSpawner : MonoBehaviour
    {
        [SerializeField] private TrackObstacle[] _obstaclePrefabs;

        [Inject] private GameService _gameService;

        private readonly List<ObstaclePool>    _pools  = new List<ObstaclePool>();
        private readonly Queue<ActiveObstacle> _active = new Queue<ActiveObstacle>();

        private float _nextSpawnZ;
        private bool  _isRunning;

        private void Start()
        {
            InitializePools();
            BindToGameService();
        }

        private void OnDestroy()
        {
            foreach (ObstaclePool pool in _pools)
                pool.Dispose();
        }

        // Each prefab carries its own placement and anchor on its TrackObstacle, so a
        // prefab can never be wired with a mismatched footprint by hand and the spawner
        // needs no parallel config.
        private void InitializePools()
        {
            foreach (TrackObstacle prefab in _obstaclePrefabs)
            {
                if (prefab == null) continue;

                _pools.Add(new ObstaclePool(prefab, transform, GameConstants.k_ObstaclePoolSizePerPrefab));
            }

            Debug.Log($"[ObstacleSpawner] {_pools.Count} pool(s) initialized");
        }

        private void BindToGameService()
        {
            _gameService.Phase
                .Subscribe(OnPhaseChanged)
                .AddTo(this);

            _gameService.CurrentDistance
                .Where(_ => _isRunning)
                .Subscribe(OnDistanceChanged)
                .AddTo(this);
        }

        private void OnPhaseChanged(GamePhase phase)
        {
            if (phase != GamePhase.Running) return;

            _isRunning = true;

            // First obstacle sits one spacing ahead of the character's start position (Z=0).
            _nextSpawnZ = GameConstants.k_ObstacleSpacing;

            // Seed the look-ahead window so obstacles are visible before the first
            // CurrentDistance tick reaches the spawn threshold.
            FillLookAhead(0f);
        }

        private void OnDistanceChanged(float characterZ)
        {
            FillLookAhead(characterZ);
            DespawnTrailing(characterZ);
        }

        private void FillLookAhead(float characterZ)
        {
            float frontier = characterZ + GameConstants.k_ObstacleSpawnLookAheadDistance;

            while (_nextSpawnZ < frontier)
            {
                SpawnNext(_nextSpawnZ);
                _nextSpawnZ += GameConstants.k_ObstacleSpacing;
            }
        }

        private void DespawnTrailing(float characterZ)
        {
            float threshold = characterZ - GameConstants.k_ObstacleDespawnBehindDistance;

            while (_active.Count > 0 && _active.Peek().SpawnZ < threshold)
            {
                ActiveObstacle obstacle = _active.Dequeue();
                obstacle.Pool.Return(obstacle.Instance);
            }
        }

        // TODO(netcode): server must select prefab/lane and broadcast via ClientRpc — this
        // local Random path desyncs across clients and moves server-side in Phase 4.
        private void SpawnNext(float spawnZ)
        {
            if (_pools.Count == 0) return;

            int poolIndex = Random.Range(0, _pools.Count);
            TrackObstacle instance = _pools[poolIndex].Rent();

            PlaceObstacle(instance, spawnZ);

            _active.Enqueue(new ActiveObstacle(instance, _pools[poolIndex], spawnZ));
        }

        private void PlaceObstacle(TrackObstacle instance, float spawnZ)
        {
            Vector3 position = instance.transform.position;

            position.x = instance.Placement == ObstaclePlacement.RandomLane
                ? GameConstants.GetLaneX(Random.Range(GameConstants.k_LaneLeft, GameConstants.k_LaneCount))
                : GameConstants.k_LaneXCenter;

            position.z = spawnZ;

            // Manual-anchored prefabs (composite coop) keep their authored Y; all others
            // derive Y from Scale so prefabs can author Pos Y = 0.
            if (instance.Anchor != ObstacleAnchor.Manual)
                position.y = GetAnchoredY(instance);

            instance.transform.position = position;
        }

        private static float GetAnchoredY(TrackObstacle instance)
        {
            float halfHeight = instance.transform.localScale.y * 0.5f;

            return instance.Anchor == ObstacleAnchor.Ceiling
                ? GameConstants.k_SlideClearanceHeight + halfHeight
                : halfHeight;
        }

        private readonly struct ActiveObstacle
        {
            public TrackObstacle Instance { get; }
            public ObstaclePool  Pool     { get; }
            public float         SpawnZ   { get; }

            public ActiveObstacle(TrackObstacle instance, ObstaclePool pool, float spawnZ)
            {
                Instance = instance;
                Pool     = pool;
                SpawnZ   = spawnZ;
            }
        }
    }
}
