using System.Collections.Generic;

using UnityEngine;

using R3;
using VContainer;

using SplitRun.Constants;
using SplitRun.Environment;
using SplitRun.Game;
using SplitRun.Item;
using SplitRun.LevelDesign;
using SplitRun.Utility;

namespace SplitRun.Obstacle
{
    // Slot contents derive from the server-owned run seed, so every client builds the same track.
    public class TrackSpawner : MonoBehaviour
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

        private readonly Dictionary<ObstacleFootprint, List<ObstaclePool>> _pools =
            new Dictionary<ObstacleFootprint, List<ObstaclePool>>();

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
            foreach (List<ObstaclePool> pools in _pools.Values)
            {
                foreach (ObstaclePool pool in pools)
                    pool.Dispose();
            }
        }

        // Prefabs are preloaded at boot and resolved by footprint, so the level profile stays theme-agnostic.
        private void InitializePools()
        {
            foreach (ObstacleFootprint footprint in _preload.Footprints)
            {
                foreach (TrackObstacle prefab in _preload.GetObstaclePrefabs(footprint))
                {
                    if (!prefab) continue;

                    AddPool(footprint, prefab);
                }
            }
        }

        private void AddPool(ObstacleFootprint footprint, TrackObstacle prefab)
        {
            if (!_pools.TryGetValue(footprint, out List<ObstaclePool> pools))
            {
                pools = new List<ObstaclePool>();
                _pools[footprint] = pools;
            }

            pools.Add(new ObstaclePool(prefab, transform, GameConstants.k_ObstaclePoolSizePerPrefab));
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

            // Build the initial look-ahead once the seed exists — during the intro when there is one —
            // so the world is already populated before the character moves. Never rebuilt on resume.
            if (_prepared || seed == 0) return;
            if (phase != GamePhase.Intro && phase != GamePhase.Running) return;

            _prepared   = true;
            _runSeed    = seed;
            _nextSpawnZ = GameConstants.k_ObstacleSpacing;

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
                SpawnSlot(_nextSpawnZ);
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

        // Obstacles first so lane occupancy is known; items then fill only free lanes.
        private void SpawnSlot(float spawnZ)
        {
            int slotIndex = Mathf.RoundToInt(spawnZ / GameConstants.k_ObstacleSpacing);

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
            foreach (ObstacleFootprintWeight entry in band.SingleWeights)
            {
                if (HasPool(entry.Footprint)) total += Mathf.Max(0f, entry.Weight);
            }

            return total;
        }

        private float AvailableCoopTotal(ObstacleBand band)
        {
            float total = 0f;
            foreach (CoopPatternWeight entry in band.CoopWeights)
            {
                if (IsPatternSpawnable(entry.Pattern)) total += Mathf.Max(0f, entry.Weight);
            }

            return total;
        }

        private void SpawnSelectedSingle(ObstacleBand band, float roll, int slotIndex, float spawnZ)
        {
            foreach (ObstacleFootprintWeight entry in band.SingleWeights)
            {
                if (!HasPool(entry.Footprint)) continue;

                roll -= Mathf.Max(0f, entry.Weight);
                if (roll > 0f) continue;

                SpawnSingle(entry.Footprint, slotIndex, spawnZ);
                return;
            }
        }

        private void SpawnSelectedCoop(ObstacleBand band, float roll, int slotIndex, float spawnZ)
        {
            foreach (CoopPatternWeight entry in band.CoopWeights)
            {
                if (!IsPatternSpawnable(entry.Pattern)) continue;

                roll -= Mathf.Max(0f, entry.Weight);
                if (roll > 0f) continue;

                SpawnCoop(entry.Pattern, slotIndex, spawnZ);
                return;
            }
        }

        private void SpawnSingle(ObstacleFootprint footprint, int slotIndex, float spawnZ)
        {
            int lane = IsFullWidth(footprint)
                ? GameConstants.k_LaneCenter
                : DeterministicRandom.NextInt(_runSeed, slotIndex, k_SaltLane,
                    GameConstants.k_LaneLeft, GameConstants.k_LaneCount);

            SpawnAt(footprint, lane, slotIndex, spawnZ);
        }

        // One random pass lane is clearable; the other two are Vertical walls, forcing both players to act.
        private void SpawnCoop(CoopPatternType pattern, int slotIndex, float spawnZ)
        {
            int passLane = DeterministicRandom.NextInt(_runSeed, slotIndex, k_SaltLane,
                GameConstants.k_LaneLeft, GameConstants.k_LaneCount);
            ObstacleFootprint passFootprint = PassFootprint(pattern);

            for (int lane = GameConstants.k_LaneLeft; lane <= GameConstants.k_LaneRight; lane++)
            {
                ObstacleFootprint footprint = lane == passLane ? passFootprint : ObstacleFootprint.Vertical;
                SpawnAt(footprint, lane, slotIndex, spawnZ);
            }
        }

        // Y stays 0 — the footprint's stamped collider center bakes the height anchor.
        private void SpawnAt(ObstacleFootprint footprint, int lane, int slotIndex, float spawnZ)
        {
            ObstaclePool pool = PickPoolForFootprint(footprint, slotIndex, lane);
            if (pool == null) return;

            TrackObstacle instance = pool.Rent();
            instance.transform.position = new Vector3(GameConstants.GetLaneX(lane), 0f, spawnZ);

            _active.Enqueue(new ActiveObstacle(instance, pool, spawnZ));
            MarkOccupied(footprint, lane);
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
                float magnetZ = spawnZ + GameConstants.k_ObstacleSpacing * 0.5f;
                _itemService.Spawn(ItemType.Magnet, new Vector3(laneX, ItemConstants.k_ItemHoverHeight, magnetZ));
                return;
            }

            PlaceCoinLine(laneX, spawnZ);
        }

        private void PlaceCoinLine(float laneX, float spawnZ)
        {
            float start = spawnZ + ItemConstants.k_CoinLineMargin;
            float end   = spawnZ + GameConstants.k_ObstacleSpacing - ItemConstants.k_CoinLineMargin;

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

        private void MarkOccupied(ObstacleFootprint footprint, int lane)
        {
            if (IsFullWidth(footprint))
            {
                for (int i = 0; i < _laneOccupied.Length; i++)
                    _laneOccupied[i] = true;
                return;
            }

            _laneOccupied[lane] = true;
        }

        private bool HasPool(ObstacleFootprint footprint) =>
            _pools.TryGetValue(footprint, out List<ObstaclePool> pools) && pools.Count > 0;

        private bool IsPatternSpawnable(CoopPatternType pattern) =>
            HasPool(ObstacleFootprint.Vertical) && HasPool(PassFootprint(pattern));

        private ObstaclePool PickPoolForFootprint(ObstacleFootprint footprint, int slotIndex, int lane)
        {
            if (!_pools.TryGetValue(footprint, out List<ObstaclePool> pools) || pools.Count == 0)
                return null;

            int variant = DeterministicRandom.NextInt(_runSeed, slotIndex, k_SaltVariantBase + lane, 0, pools.Count);
            return pools[variant];
        }

        private static ObstacleFootprint PassFootprint(CoopPatternType pattern) => pattern switch
        {
            CoopPatternType.CoopSlide => ObstacleFootprint.LaneSlide,
            _ => ObstacleFootprint.LaneJump,
        };

        private static bool IsFullWidth(ObstacleFootprint footprint) =>
            footprint == ObstacleFootprint.WideJump || footprint == ObstacleFootprint.WideSlide;

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
