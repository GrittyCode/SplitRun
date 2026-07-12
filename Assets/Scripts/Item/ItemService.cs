using System;
using System.Collections.Generic;

using UnityEngine;

using R3;
using Unity.Netcode;
using VContainer.Unity;

using SplitRun.Character;
using SplitRun.Constants;
using SplitRun.Data;
using SplitRun.Game;
using SplitRun.Mission;
using SplitRun.Utility;

namespace SplitRun.Item
{
    public sealed class ItemService : IStartable, ITickable, IDisposable
    {
        private readonly ItemCatalog       _catalog;
        private readonly GameService       _gameService;
        private readonly GameSession       _gameSession;
        private readonly PlayerDataService _playerDataService;
        private readonly MissionService    _missionService;

        private readonly Dictionary<ItemType, ComponentPool<ItemPickup>> _pools =
            new Dictionary<ItemType, ComponentPool<ItemPickup>>();

        private readonly List<ItemPickup> _active = new List<ItemPickup>();

        // A coin latched while the magnet is active keeps homing until collected, even after expiry.
        private readonly HashSet<ItemPickup> _pulled = new HashSet<ItemPickup>();

        private readonly ReactiveProperty<int>   _coins           = new ReactiveProperty<int>(0);
        private readonly ReactiveProperty<float> _magnetRemaining = new ReactiveProperty<float>(0f);

        private DisposableBag _disposables;
        private Transform     _root;
        private ICharacter    _character;
        private float         _magnetSeconds;
        private int           _nextSpawnId;

        public ItemService(ItemCatalog catalog, GameService gameService, GameSession gameSession,
            PlayerDataService playerDataService, MissionService missionService)
        {
            _catalog           = catalog;
            _gameService       = gameService;
            _gameSession       = gameSession;
            _playerDataService = playerDataService;
            _missionService    = missionService;
        }

        public ReadOnlyReactiveProperty<int>   Coins           => _coins;
        public ReadOnlyReactiveProperty<float> MagnetRemaining => _magnetRemaining;

        public void Start()
        {
            _root = new GameObject("[Items]").transform;

            foreach ((ItemType type, ItemPickup prefab) in _catalog.Prefabs)
                BuildPool(type, prefab, _catalog.PoolSizeFor(type));

            CharacterEvents.OnSpawned   += OnCharacterSpawned;
            CharacterEvents.OnDespawned += OnCharacterDespawned;
            ItemPickup.OnCollected      += OnItemCollected;

            _gameService.Phase
                .Subscribe(OnPhaseChanged)
                .AddTo(ref _disposables);

            _gameSession.OnItemCollectionConfirmed
                .Subscribe(OnCollectionConfirmed)
                .AddTo(ref _disposables);
        }

        public void Tick()
        {
            SpinActiveItems();

            if (_character == null) return;
            if (_gameService.Phase.CurrentValue != GamePhase.Running) return;

            UpdateMagnet(Time.deltaTime);
            RecycleTrailing();
        }

        public void Dispose()
        {
            CharacterEvents.OnSpawned   -= OnCharacterSpawned;
            CharacterEvents.OnDespawned -= OnCharacterDespawned;
            ItemPickup.OnCollected      -= OnItemCollected;

            foreach (ComponentPool<ItemPickup> pool in _pools.Values)
                pool.Dispose();

            _disposables.Dispose();
            _coins.Dispose();
            _magnetRemaining.Dispose();

            if (_root)
                UnityEngine.Object.Destroy(_root.gameObject);
        }

        // Spawn order is deterministic across clients, so the running id names the same pickup everywhere.
        public void Spawn(ItemType type, Vector3 position)
        {
            if (!_pools.TryGetValue(type, out ComponentPool<ItemPickup> pool)) return;

            ItemPickup pickup = pool.Rent();
            pickup.Initialize(_nextSpawnId++);
            pickup.transform.position = position;
            _active.Add(pickup);
        }

        // The service is scene-scoped, so a run starts from a fresh instance — no reset path exists,
        // and pre-run look-ahead items must survive into Running.
        private void OnPhaseChanged(GamePhase phase)
        {
            // GameOver is entered once per run, so the merge never double-applies.
            if (phase == GamePhase.GameOver)
                MergeCoins();
        }

        private void MergeCoins()
        {
            _playerDataService.AddCoins(_coins.Value);
            _missionService.ReportRunCoins(_coins.Value);
        }

        private void OnCharacterSpawned(ICharacter character) => _character = character;

        private void OnCharacterDespawned(ICharacter character)
        {
            if (_character == character) _character = null;
        }

        // Only the server's trigger is authoritative; every client applies the confirmed broadcast.
        private void OnItemCollected(ItemPickup item)
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (!networkManager || !networkManager.IsServer) return;

            if (!_active.Contains(item)) return;

            _gameSession.ConfirmItemCollected(item.SpawnId);
        }

        private void OnCollectionConfirmed(int spawnId)
        {
            ItemPickup item = FindActive(spawnId);
            if (!item) return;

            _active.Remove(item);

            switch (item.Type)
            {
                case ItemType.Coin:
                    _coins.Value += ItemConstants.k_CoinValue;
                    break;
                case ItemType.Magnet:
                    _magnetSeconds = ItemConstants.k_MagnetDuration;
                    break;
            }

            Recycle(item);
        }

        private ItemPickup FindActive(int spawnId)
        {
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i].SpawnId == spawnId) return _active[i];
            }

            return null;
        }

        // Driven here instead of a per-pickup Update — dozens of live coins would each pay the per-MonoBehaviour call cost.
        private void SpinActiveItems()
        {
            float step = ItemConstants.k_SpinSpeed * Time.deltaTime;

            for (int i = 0; i < _active.Count; i++)
                _active[i].transform.Rotate(0f, step, 0f, Space.World);
        }

        private void UpdateMagnet(float deltaTime)
        {
            bool isActive = _magnetSeconds > 0f;
            if (isActive)
            {
                _magnetSeconds         = Mathf.Max(0f, _magnetSeconds - deltaTime);
                _magnetRemaining.Value = _magnetSeconds;
            }

            if (_pulled.Count == 0 && !isActive) return;

            Vector3 target = _character.CharacterTransform.position + Vector3.up * ItemConstants.k_ItemHoverHeight;

            if (isActive) LatchCoinsInRange(target);
            PullLatched(target, deltaTime);
        }

        private void LatchCoinsInRange(Vector3 target)
        {
            float radiusSqr = ItemConstants.k_MagnetRadius * ItemConstants.k_MagnetRadius;

            foreach (ItemPickup pickup in _active)
            {
                if (pickup.Type != ItemType.Coin || _pulled.Contains(pickup)) continue;
                if ((pickup.transform.position - target).sqrMagnitude > radiusSqr) continue;

                _pulled.Add(pickup);
            }
        }

        private void PullLatched(Vector3 target, float deltaTime)
        {
            float step = ItemConstants.k_MagnetPullSpeed * deltaTime;

            foreach (ItemPickup pickup in _pulled)
                pickup.transform.position = Vector3.MoveTowards(pickup.transform.position, target, step);
        }

        private void RecycleTrailing()
        {
            float threshold = _character.CharacterTransform.position.z - ItemConstants.k_ItemDespawnBehindDistance;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                ItemPickup pickup = _active[i];
                if (pickup.transform.position.z >= threshold) continue;

                Recycle(pickup);
                _active.RemoveAt(i);
            }
        }

        private void BuildPool(ItemType type, ItemPickup prefab, int size)
        {
            if (!prefab)
            {
                Debug.LogWarning($"[ItemService] No prefab for {type} — spawning disabled for it.");
                return;
            }

            _pools[type] = new ComponentPool<ItemPickup>(prefab, _root, size, pickup => pickup.ResetState());
        }

        private void Recycle(ItemPickup pickup)
        {
            _pulled.Remove(pickup);

            if (_pools.TryGetValue(pickup.Type, out ComponentPool<ItemPickup> pool))
                pool.Return(pickup);
        }
    }
}
