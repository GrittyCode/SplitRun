using System;
using System.Collections.Generic;
using System.Threading;

using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

using Cysharp.Threading.Tasks;

using SplitRun.Environment;
using SplitRun.Obstacle;

namespace SplitRun.Boot
{
    public sealed class AssetPreloadService : IDisposable
    {
        private readonly WorldThemeProfile _theme;

        private readonly Dictionary<ObstacleFootprint, List<TrackObstacle>> _obstaclePrefabs =
            new Dictionary<ObstacleFootprint, List<TrackObstacle>>();

        private readonly List<AsyncOperationHandle<GameObject>> _handles =
            new List<AsyncOperationHandle<GameObject>>();

        private bool _isLoaded;

        public AssetPreloadService(WorldThemeProfile theme) => _theme = theme;

        public IReadOnlyCollection<ObstacleFootprint> Footprints => _obstaclePrefabs.Keys;

        public IReadOnlyList<TrackObstacle> GetObstaclePrefabs(ObstacleFootprint footprint) =>
            _obstaclePrefabs.TryGetValue(footprint, out List<TrackObstacle> prefabs)
                ? prefabs
                : Array.Empty<TrackObstacle>();

        /// <summary>Resolves the theme's obstacle prefabs once; safe to call again as a no-op.</summary>
        public async UniTask LoadAsync(CancellationToken ct)
        {
            if (_isLoaded) return;

            if (!_theme)
            {
                Debug.LogWarning("[AssetPreloadService] No world theme — obstacle spawning will be disabled.");
                _isLoaded = true;
                return;
            }

            await LoadObstaclePrefabsAsync(ct);

            _isLoaded = true;
            Debug.Log($"[AssetPreloadService] Loaded obstacle prefabs for {_obstaclePrefabs.Count} footprint(s).");
        }

        public void Dispose()
        {
            foreach (AsyncOperationHandle<GameObject> handle in _handles)
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
            }

            _handles.Clear();
            _obstaclePrefabs.Clear();
        }

        // Registration follows the theme's declared order, so the seed-derived variant index resolves identically everywhere.
        private async UniTask LoadObstaclePrefabsAsync(CancellationToken ct)
        {
            var footprints = new List<ObstacleFootprint>();
            var tasks      = new List<UniTask<GameObject>>();

            foreach ((ObstacleFootprint footprint, ObstacleVariants variants) in _theme.ObstaclePrefabs)
            {
                if (variants?.Prefabs == null) continue;

                foreach (AssetReferenceGameObject reference in variants.Prefabs)
                {
                    if (reference == null || !reference.RuntimeKeyIsValid()) continue;

                    // Loaded through Addressables, not the reference's cached handle, so the shared theme SO never owns these handles.
                    AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(reference);
                    _handles.Add(handle);
                    footprints.Add(footprint);
                    tasks.Add(handle.ToUniTask(cancellationToken: ct));
                }
            }

            if (tasks.Count == 0) return;

            GameObject[] loaded = await UniTask.WhenAll(tasks);

            for (int i = 0; i < loaded.Length; i++)
                Register(footprints[i], loaded[i]);
        }

        private void Register(ObstacleFootprint footprint, GameObject prefab)
        {
            if (!prefab) return;

            if (!prefab.TryGetComponent(out TrackObstacle obstacle))
            {
                Debug.LogWarning($"[AssetPreloadService] Addressable '{prefab.name}' has no TrackObstacle — skipped.");
                return;
            }

            if (!_obstaclePrefabs.TryGetValue(footprint, out List<TrackObstacle> prefabs))
            {
                prefabs = new List<TrackObstacle>();
                _obstaclePrefabs[footprint] = prefabs;
            }

            prefabs.Add(obstacle);
        }
    }
}
