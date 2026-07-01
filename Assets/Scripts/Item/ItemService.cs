using System;
using System.Collections.Generic;

using UnityEngine;

using R3;
using VContainer.Unity;

using SplitRun.Character;
using SplitRun.Constants;
using SplitRun.Game;

namespace SplitRun.Item
{
    // Owns the pickup run lifetime (pool, placement, magnet, despawn, collection); coins are local UI state.
    public sealed class ItemService : IStartable, ITickable, IDisposable
    {
        private readonly ItemCatalog _catalog;
        private readonly GameService _gameService;

        private readonly Dictionary<ItemType, ItemPickup>        _prefabs = new Dictionary<ItemType, ItemPickup>();
        private readonly Dictionary<ItemType, Queue<ItemPickup>> _idle    = new Dictionary<ItemType, Queue<ItemPickup>>();
        private readonly List<ItemPickup>                        _active  = new List<ItemPickup>();

        // Coins latched while the magnet is active keep being pulled until collected even after
        // it expires, so none freeze mid-air when the duration ends.
        private readonly HashSet<ItemPickup> _pulled = new HashSet<ItemPickup>();

        private readonly ReactiveProperty<int>   _coins           = new ReactiveProperty<int>(0);
        private readonly ReactiveProperty<float> _magnetRemaining = new ReactiveProperty<float>(0f);

        private DisposableBag _disposables;
        private Transform     _root;
        private ICharacter    _character;
        private float         _magnetSeconds;

        public ItemService(ItemCatalog catalog, GameService gameService)
        {
            _catalog     = catalog;
            _gameService = gameService;
        }

        public ReadOnlyReactiveProperty<int>   Coins           => _coins;
        public ReadOnlyReactiveProperty<float> MagnetRemaining => _magnetRemaining;

        public void Start()
        {
            _root = new GameObject("[Items]").transform;

            BuildPool(ItemType.Coin,   _catalog.CoinPrefab,   ItemConstants.k_CoinPoolSize);
            BuildPool(ItemType.Magnet, _catalog.MagnetPrefab, ItemConstants.k_MagnetPoolSize);

            CharacterEvents.OnSpawned   += OnCharacterSpawned;
            CharacterEvents.OnDespawned += OnCharacterDespawned;
            ItemEvents.OnCollected      += OnItemCollected;

            _gameService.Phase
                .Where(phase => phase == GamePhase.Running)
                .Subscribe(_ => ResetForRun())
                .AddTo(ref _disposables);
        }

        public void Tick()
        {
            if (_character == null) return;

            UpdateMagnet(Time.deltaTime);
            RecycleTrailing();
        }

        public void Dispose()
        {
            CharacterEvents.OnSpawned   -= OnCharacterSpawned;
            CharacterEvents.OnDespawned -= OnCharacterDespawned;
            ItemEvents.OnCollected      -= OnItemCollected;

            _disposables.Dispose();
            _coins.Dispose();
            _magnetRemaining.Dispose();

            if (_root)
                UnityEngine.Object.Destroy(_root.gameObject);
        }

        // TODO(netcode): server selects/places and broadcasts pickups; local placement desyncs clients.
        public void Spawn(ItemType type, Vector3 position)
        {
            ItemPickup pickup = Rent(type);
            if (!pickup) return;

            pickup.transform.position = position;
            _active.Add(pickup);
        }

        private void ResetForRun()
        {
            _coins.Value           = 0;
            _magnetSeconds         = 0f;
            _magnetRemaining.Value = 0f;

            for (int i = _active.Count - 1; i >= 0; i--)
                Recycle(_active[i]);

            _active.Clear();
            _pulled.Clear();
        }

        private void OnCharacterSpawned(ICharacter character) => _character = character;

        private void OnCharacterDespawned(ICharacter character)
        {
            if (_character == character) _character = null;
        }

        private void OnItemCollected(ItemPickup item)
        {
            if (!_active.Remove(item)) return;

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
            float threshold = _character.CharacterTransform.position.z - GameConstants.k_ObstacleDespawnBehindDistance;

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

            _prefabs[type] = prefab;

            Queue<ItemPickup> queue = new Queue<ItemPickup>(size);
            for (int i = 0; i < size; i++)
            {
                ItemPickup instance = Create(prefab);
                instance.gameObject.SetActive(false);
                queue.Enqueue(instance);
            }

            _idle[type] = queue;
        }

        private ItemPickup Rent(ItemType type)
        {
            if (!_idle.TryGetValue(type, out Queue<ItemPickup> queue) ||
                !_prefabs.TryGetValue(type, out ItemPickup prefab))
                return null;

            ItemPickup pickup = queue.Count > 0 ? queue.Dequeue() : Create(prefab);
            pickup.ResetState();
            return pickup;
        }

        private void Recycle(ItemPickup pickup)
        {
            _pulled.Remove(pickup);
            pickup.gameObject.SetActive(false);
            Enqueue(pickup);
        }

        private void Enqueue(ItemPickup pickup)
        {
            if (_idle.TryGetValue(pickup.Type, out Queue<ItemPickup> queue))
                queue.Enqueue(pickup);
        }

        private ItemPickup Create(ItemPickup prefab) => UnityEngine.Object.Instantiate(prefab, _root);
    }
}
