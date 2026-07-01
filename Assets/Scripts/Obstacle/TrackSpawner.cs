using System.Collections.Generic;

using UnityEngine;

using R3;
using VContainer;

using SplitRun.Constants;
using SplitRun.Environment;
using SplitRun.Game;
using SplitRun.Item;
using SplitRun.LevelDesign;

namespace SplitRun.Obstacle
{
    public class TrackSpawner : MonoBehaviour
    {
        [Inject] private GameService        _gameService;
        [Inject] private LevelDesignProfile _levelProfile;
        [Inject] private WorldThemeProfile  _theme;
        [Inject] private ItemService        _itemService;

        private readonly List<ObstaclePool>     _pools  = new List<ObstaclePool>();
        private readonly Queue<ActiveObstacle>  _active = new Queue<ActiveObstacle>();

        private readonly Dictionary<ObstacleFootprint, List<int>> _poolsByFootprint =
            new Dictionary<ObstacleFootprint, List<int>>();

        private readonly bool[] _laneOccupied = new bool[GameConstants.k_LaneCount];
        private readonly int[]  _freeLanes    = new int[GameConstants.k_LaneCount];

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

        // Pools are built from the active theme's footprint-to-prefab map, so the level profile
        // stays theme-agnostic: it weights footprints, the theme supplies the meshes.
        private void InitializePools()
        {
            if (!_theme)
            {
                Debug.LogWarning("[TrackSpawner] No world theme — spawning disabled.");
                return;
            }

            foreach (FootprintPrefabs set in _theme.ObstaclePrefabs)
                RegisterPrefabSet(set);

            Debug.Log($"[TrackSpawner] {_pools.Count} pool(s) initialized");
        }

        private void RegisterPrefabSet(FootprintPrefabs set)
        {
            if (set.Prefabs == null) return;

            foreach (TrackObstacle prefab in set.Prefabs)
            {
                if (!prefab) continue;

                if (prefab.Footprint != set.Footprint)
                    Debug.LogWarning($"[TrackSpawner] '{prefab.name}' footprint {prefab.Footprint} " +
                                     $"does not match its theme slot {set.Footprint}.");

                int poolIndex = _pools.Count;
                _pools.Add(new ObstaclePool(prefab, transform, GameConstants.k_ObstaclePoolSizePerPrefab));
                RegisterFootprint(set.Footprint, poolIndex);
            }
        }

        private void RegisterFootprint(ObstacleFootprint footprint, int poolIndex)
        {
            if (!_poolsByFootprint.TryGetValue(footprint, out List<int> indices))
            {
                indices = new List<int>();
                _poolsByFootprint[footprint] = indices;
            }

            indices.Add(poolIndex);
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

            _isRunning  = true;
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

        // Obstacles are placed first so lane occupancy is known; items then fill the gap to the
        // next slot in a free lane, guaranteeing pickups never sit on an obstacle.
        private void SpawnSlot(float spawnZ)
        {
            ClearOccupancy();

            if (_pools.Count > 0 && _levelProfile && _levelProfile.HasBands)
                SpawnObstacles(spawnZ);

            PlaceItems(spawnZ);
        }

        // One weighted roll over both single obstacles and coop patterns: difficulty is read at the
        // obstacle's own spawn Z (where the player meets it), not the character's current Z.
        // TODO(netcode): server must select the slot contents and broadcast via ClientRpc.
        private void SpawnObstacles(float spawnZ)
        {
            ObstacleBand band = _levelProfile.ResolveBand(spawnZ);

            float singleTotal = AvailableSingleTotal(band);
            float total       = singleTotal + AvailableCoopTotal(band);
            if (total <= 0f) return;

            float roll = Random.value * total;
            if (roll < singleTotal)
                SpawnSelectedSingle(band, roll, spawnZ);
            else
                SpawnSelectedCoop(band, roll - singleTotal, spawnZ);
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

        private void SpawnSelectedSingle(ObstacleBand band, float roll, float spawnZ)
        {
            foreach (ObstacleFootprintWeight entry in band.SingleWeights)
            {
                if (!HasPool(entry.Footprint)) continue;

                roll -= Mathf.Max(0f, entry.Weight);
                if (roll > 0f) continue;

                SpawnSingle(entry.Footprint, spawnZ);
                return;
            }
        }

        private void SpawnSelectedCoop(ObstacleBand band, float roll, float spawnZ)
        {
            foreach (CoopPatternWeight entry in band.CoopWeights)
            {
                if (!IsPatternSpawnable(entry.Pattern)) continue;

                roll -= Mathf.Max(0f, entry.Weight);
                if (roll > 0f) continue;

                SpawnCoop(entry.Pattern, spawnZ);
                return;
            }
        }

        private void SpawnSingle(ObstacleFootprint footprint, float spawnZ)
        {
            int lane = IsFullWidth(footprint)
                ? GameConstants.k_LaneCenter
                : Random.Range(GameConstants.k_LaneLeft, GameConstants.k_LaneCount);

            SpawnAt(footprint, lane, spawnZ);
        }

        // Every lane filled at the same Z: one random pass lane gets the clearable footprint, the
        // other two are Vertical walls, so the slot demands both a lane change and a vertical move.
        private void SpawnCoop(CoopPatternType pattern, float spawnZ)
        {
            int passLane = Random.Range(GameConstants.k_LaneLeft, GameConstants.k_LaneCount);
            ObstacleFootprint passFootprint = PassFootprint(pattern);

            for (int lane = GameConstants.k_LaneLeft; lane <= GameConstants.k_LaneRight; lane++)
            {
                ObstacleFootprint footprint = lane == passLane ? passFootprint : ObstacleFootprint.Vertical;
                SpawnAt(footprint, lane, spawnZ);
            }
        }

        // Y stays 0 — the footprint's collider center bakes the ground/head-height anchor, so
        // placement only chooses the lane X.
        private void SpawnAt(ObstacleFootprint footprint, int lane, float spawnZ)
        {
            int poolIndex = PickPoolForFootprint(footprint);
            if (poolIndex < 0) return;

            TrackObstacle instance = _pools[poolIndex].Rent();
            instance.transform.position = new Vector3(GameConstants.GetLaneX(lane), 0f, spawnZ);

            _active.Enqueue(new ActiveObstacle(instance, _pools[poolIndex], spawnZ));
            MarkOccupied(footprint, lane);
        }

        private void PlaceItems(float spawnZ)
        {
            if (_itemService == null || !_levelProfile) return;

            float chance = ItemConstants.k_CoinLineChance * _levelProfile.CoinSpawnMultiplier;
            if (Random.value > chance) return;

            int lane = PickFreeLane();
            if (lane < 0) return;

            float laneX = GameConstants.GetLaneX(lane);

            if (Random.value < ItemConstants.k_MagnetChance)
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

        private int PickFreeLane()
        {
            int count = 0;
            for (int lane = 0; lane < GameConstants.k_LaneCount; lane++)
            {
                if (!_laneOccupied[lane]) _freeLanes[count++] = lane;
            }

            return count == 0 ? -1 : _freeLanes[Random.Range(0, count)];
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
            _poolsByFootprint.TryGetValue(footprint, out List<int> indices) && indices.Count > 0;

        private bool IsPatternSpawnable(CoopPatternType pattern) =>
            HasPool(ObstacleFootprint.Vertical) && HasPool(PassFootprint(pattern));

        private int PickPoolForFootprint(ObstacleFootprint footprint)
        {
            if (!_poolsByFootprint.TryGetValue(footprint, out List<int> indices) || indices.Count == 0)
                return -1;

            return indices[Random.Range(0, indices.Count)];
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
