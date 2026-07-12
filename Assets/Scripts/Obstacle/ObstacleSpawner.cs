using System.Collections.Generic;

using UnityEngine;

using R3;
using VContainer;

using SplitRun.Boot;
using SplitRun.Constants;
using SplitRun.Game;
using SplitRun.Item;
using SplitRun.LevelDesign;
using SplitRun.Utility;

namespace SplitRun.Obstacle
{
    // Slot contents derive from the server-owned run seed, so every client builds the same track.
    public class ObstacleSpawner : MonoBehaviour
    {
        // Salt spaces per decision within a slot; variant salts add the lane so coop lanes differ.
        private const int k_SaltObstacleRoll = 0;
        private const int k_SaltLane         = 1;
        private const int k_SaltVariantBase  = 10;
        private const int k_SaltItemRoll     = 20;
        private const int k_SaltItemLane     = 21;
        private const int k_SaltMagnetRoll   = 22;

        [Inject] private GameService         _gameService;
        [Inject] private GameSession         _gameSession;
        [Inject] private LevelDesignProfile  _levelProfile;
        [Inject] private AssetPreloadService _preload;
        [Inject] private ItemService         _itemService;

        private readonly Dictionary<ObstacleType, List<ComponentPool<TrackObstacle>>> _pools =
            new Dictionary<ObstacleType, List<ComponentPool<TrackObstacle>>>();

        private readonly Queue<ActiveObstacle> _active = new Queue<ActiveObstacle>();

        private readonly bool[] _laneOccupied = new bool[GameConstants.k_LaneCount];
        private readonly int[]  _freeLanes    = new int[GameConstants.k_LaneCount];

        private float _nextSpawnZ;
        private bool  _isRunning;
        private bool  _prepared;
        private int   _runSeed;

        private void Start()
        {
            InitializePools();
            BindToGameService();
        }

        private void OnDestroy()
        {
            foreach (List<ComponentPool<TrackObstacle>> pools in _pools.Values)
            {
                foreach (ComponentPool<TrackObstacle> pool in pools)
                    pool.Dispose();
            }
        }

        // Prefabs are preloaded at boot and resolved by obstacle type, so the level profile stays theme-agnostic.
        private void InitializePools()
        {
            foreach (ObstacleType type in _preload.ObstacleTypes)
            {
                foreach (TrackObstacle prefab in _preload.GetObstaclePrefabs(type))
                {
                    if (!prefab) continue;

                    AddPool(type, prefab);
                }
            }
        }

        private void AddPool(ObstacleType type, TrackObstacle prefab)
        {
            if (!_pools.TryGetValue(type, out List<ComponentPool<TrackObstacle>> pools))
            {
                pools = new List<ComponentPool<TrackObstacle>>();
                _pools[type] = pools;
            }

            pools.Add(new ComponentPool<TrackObstacle>(
                prefab, transform, ObstacleConstants.k_ObstaclePoolSizePerPrefab, obstacle => obstacle.ResetState()));
        }

        private void BindToGameService()
        {
            _gameService.Phase
                .CombineLatest(_gameSession.RunSeed, (phase, seed) => (phase, seed))
                .Subscribe(pair => OnRunStateChanged(pair.phase, pair.seed))
                .AddTo(this);

            _gameService.CurrentDistance
                .Where(_ => _isRunning)
                .Subscribe(OnDistanceChanged)
                .AddTo(this);
        }

        private void OnRunStateChanged(GamePhase phase, int seed)
        {
            _isRunning = phase == GamePhase.Running && seed != 0;

            // The seed ships in GameSession's spawn payload, so the world is built the moment the scene
            // syncs — never waiting on the ready handshake that gates the run itself.
            if (_prepared || seed == 0) return;

            _prepared   = true;
            _runSeed    = seed;
            _nextSpawnZ = ObstacleConstants.k_ObstacleSpacing;

            FillLookAhead(0f);
        }

        private void OnDistanceChanged(float characterZ)
        {
            FillLookAhead(characterZ);
            DespawnTrailing(characterZ);
        }

        private void FillLookAhead(float characterZ)
        {
            float frontier = characterZ + ObstacleConstants.k_ObstacleSpawnLookAheadDistance;

            while (_nextSpawnZ < frontier)
            {
                SpawnSlot(_nextSpawnZ);
                _nextSpawnZ += ObstacleConstants.k_ObstacleSpacing;
            }
        }

        private void DespawnTrailing(float characterZ)
        {
            float threshold = characterZ - ObstacleConstants.k_ObstacleDespawnBehindDistance;

            while (_active.Count > 0 && _active.Peek().SpawnZ < threshold)
            {
                ActiveObstacle obstacle = _active.Dequeue();
                obstacle.Pool.Return(obstacle.Instance);
            }
        }

        // Obstacles first so lane occupancy is known; items then fill only free lanes.
        private void SpawnSlot(float spawnZ)
        {
            int slotIndex = Mathf.RoundToInt(spawnZ / ObstacleConstants.k_ObstacleSpacing);

            ClearOccupancy();

            if (_pools.Count > 0 && _levelProfile && _levelProfile.HasBands)
                SpawnObstacles(slotIndex, spawnZ);

            PlaceItems(slotIndex, spawnZ);
        }

        // Difficulty is read at the obstacle's own spawn Z (where the player meets it), not the character's Z.
        private void SpawnObstacles(int slotIndex, float spawnZ)
        {
            ObstacleBand band = _levelProfile.ResolveBand(spawnZ);

            float singleTotal = AvailableSingleTotal(band);
            float total       = singleTotal + AvailableCoopTotal(band);
            if (total <= 0f) return;

            float roll = DeterministicRandom.NextFloat(_runSeed, slotIndex, k_SaltObstacleRoll) * total;
            if (roll < singleTotal)
                SpawnSelectedSingle(band, roll, slotIndex, spawnZ);
            else
                SpawnSelectedCoop(band, roll - singleTotal, slotIndex, spawnZ);
        }

        private float AvailableSingleTotal(ObstacleBand band)
        {
            float total = 0f;
            foreach ((ObstacleType type, float weight) in band.SingleWeights)
            {
                if (!IsSingleSpawnable(type, weight)) continue;

                total += weight;
            }

            return total;
        }

        private float AvailableCoopTotal(ObstacleBand band)
        {
            float total = 0f;
            foreach ((CoopPatternType pattern, float weight) in band.CoopWeights)
            {
                if (!IsCoopSpawnable(pattern, weight)) continue;

                total += weight;
            }

            return total;
        }

        private void SpawnSelectedSingle(ObstacleBand band, float roll, int slotIndex, float spawnZ)
        {
            foreach ((ObstacleType type, float weight) in band.SingleWeights)
            {
                if (!IsSingleSpawnable(type, weight)) continue;

                roll -= weight;
                if (roll > 0f) continue;

                SpawnSingle(type, slotIndex, spawnZ);
                return;
            }
        }

        private void SpawnSelectedCoop(ObstacleBand band, float roll, int slotIndex, float spawnZ)
        {
            foreach ((CoopPatternType pattern, float weight) in band.CoopWeights)
            {
                if (!IsCoopSpawnable(pattern, weight)) continue;

                roll -= weight;
                if (roll > 0f) continue;

                SpawnCoop(pattern, slotIndex, spawnZ);
                return;
            }
        }

        private void SpawnSingle(ObstacleType type, int slotIndex, float spawnZ)
        {
            int lane = type.IsFullWidth()
                ? GameConstants.k_LaneCenter
                : DeterministicRandom.NextInt(_runSeed, slotIndex, k_SaltLane,
                    GameConstants.k_LaneLeft, GameConstants.k_LaneCount);

            SpawnAt(type, lane, slotIndex, spawnZ);
        }

        // One random pass lane is clearable; the other two are Vertical walls, forcing both players to act.
        private void SpawnCoop(CoopPatternType pattern, int slotIndex, float spawnZ)
        {
            int passLane = DeterministicRandom.NextInt(_runSeed, slotIndex, k_SaltLane,
                GameConstants.k_LaneLeft, GameConstants.k_LaneCount);
            ObstacleType passType = PassObstacleType(pattern);

            for (int lane = GameConstants.k_LaneLeft; lane <= GameConstants.k_LaneRight; lane++)
            {
                ObstacleType type = lane == passLane ? passType : ObstacleType.Vertical;
                SpawnAt(type, lane, slotIndex, spawnZ);
            }
        }

        // Y stays 0 — the obstacle type's stamped collider center bakes the height anchor.
        private void SpawnAt(ObstacleType type, int lane, int slotIndex, float spawnZ)
        {
            ComponentPool<TrackObstacle> pool = PickPoolForType(type, slotIndex, lane);
            if (pool == null) return;

            TrackObstacle instance = pool.Rent();
            instance.transform.position = new Vector3(GameConstants.GetLaneX(lane), 0f, spawnZ);

            _active.Enqueue(new ActiveObstacle(instance, pool, spawnZ));
            MarkOccupied(type, lane);
        }

        private void PlaceItems(int slotIndex, float spawnZ)
        {
            if (_itemService == null || !_levelProfile) return;

            float chance = ItemConstants.k_CoinLineChance * _levelProfile.CoinSpawnMultiplier;
            if (DeterministicRandom.NextFloat(_runSeed, slotIndex, k_SaltItemRoll) > chance) return;

            int lane = PickFreeLane(slotIndex);
            if (lane < 0) return;

            float laneX = GameConstants.GetLaneX(lane);

            if (DeterministicRandom.NextFloat(_runSeed, slotIndex, k_SaltMagnetRoll) < ItemConstants.k_MagnetChance)
            {
                float magnetZ = spawnZ + ObstacleConstants.k_ObstacleSpacing * 0.5f;
                _itemService.Spawn(ItemType.Magnet, new Vector3(laneX, ItemConstants.k_ItemHoverHeight, magnetZ));
                return;
            }

            PlaceCoinLine(laneX, spawnZ);
        }

        private void PlaceCoinLine(float laneX, float spawnZ)
        {
            float start = spawnZ + ItemConstants.k_CoinLineMargin;
            float end   = spawnZ + ObstacleConstants.k_ObstacleSpacing - ItemConstants.k_CoinLineMargin;

            for (float z = start; z <= end; z += ItemConstants.k_CoinSpacing)
                _itemService.Spawn(ItemType.Coin, new Vector3(laneX, ItemConstants.k_ItemHoverHeight, z));
        }

        private int PickFreeLane(int slotIndex)
        {
            int count = 0;
            for (int lane = 0; lane < GameConstants.k_LaneCount; lane++)
            {
                if (!_laneOccupied[lane]) _freeLanes[count++] = lane;
            }

            return count == 0
                ? -1
                : _freeLanes[DeterministicRandom.NextInt(_runSeed, slotIndex, k_SaltItemLane, 0, count)];
        }

        private void ClearOccupancy()
        {
            for (int i = 0; i < _laneOccupied.Length; i++)
                _laneOccupied[i] = false;
        }

        private void MarkOccupied(ObstacleType type, int lane)
        {
            if (type.IsFullWidth())
            {
                for (int i = 0; i < _laneOccupied.Length; i++)
                    _laneOccupied[i] = true;
                return;
            }

            _laneOccupied[lane] = true;
        }

        private bool HasPool(ObstacleType type) =>
            _pools.TryGetValue(type, out List<ComponentPool<TrackObstacle>> pools) && pools.Count > 0;

        // A zero weight is an unused row of the enum-keyed table, not a spawnable choice.
        private bool IsSingleSpawnable(ObstacleType type, float weight) =>
            weight > 0f && HasPool(type);

        private bool IsCoopSpawnable(CoopPatternType pattern, float weight) =>
            weight > 0f && HasPool(ObstacleType.Vertical) && HasPool(PassObstacleType(pattern));

        private ComponentPool<TrackObstacle> PickPoolForType(ObstacleType type, int slotIndex, int lane)
        {
            if (!_pools.TryGetValue(type, out List<ComponentPool<TrackObstacle>> pools) || pools.Count == 0)
                return null;

            int variant = DeterministicRandom.NextInt(_runSeed, slotIndex, k_SaltVariantBase + lane, 0, pools.Count);
            return pools[variant];
        }

        private static ObstacleType PassObstacleType(CoopPatternType pattern) => pattern switch
        {
            CoopPatternType.CoopSlide => ObstacleType.LaneSlide,
            _                         => ObstacleType.LaneJump,
        };

        private readonly struct ActiveObstacle
        {
            public TrackObstacle                Instance { get; }
            public ComponentPool<TrackObstacle> Pool     { get; }
            public float                        SpawnZ   { get; }

            public ActiveObstacle(TrackObstacle instance, ComponentPool<TrackObstacle> pool, float spawnZ)
            {
                Instance = instance;
                Pool     = pool;
                SpawnZ   = spawnZ;
            }
        }
    }
}
