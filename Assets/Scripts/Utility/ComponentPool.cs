using System;
using System.Collections.Generic;

using UnityEngine;

namespace SplitRun.Utility
{
    // One pooling implementation for every recycled prefab instance in the project.
    public sealed class ComponentPool<T> : IDisposable where T : Component
    {
        private readonly T           _prefab;
        private readonly Transform   _parent;
        private readonly Action<T>   _onRent;
        private readonly Queue<T>    _idle;

        // onRent is captured once here — a per-Rent lambda would allocate on the spawn hot path.
        public ComponentPool(T prefab, Transform parent, int initialSize, Action<T> onRent = null)
        {
            _prefab = prefab;
            _parent = parent;
            _onRent = onRent;
            _idle   = new Queue<T>(initialSize);

            Prewarm(initialSize);
        }

        public T Rent()
        {
            T instance = _idle.Count > 0 ? _idle.Dequeue() : Create();

            if (_onRent != null)
                _onRent(instance);
            else
                instance.gameObject.SetActive(true);

            return instance;
        }

        public void Return(T instance)
        {
            instance.gameObject.SetActive(false);
            _idle.Enqueue(instance);
        }

        public void Dispose()
        {
            while (_idle.Count > 0)
            {
                T instance = _idle.Dequeue();
                if (instance)
                    UnityEngine.Object.Destroy(instance.gameObject);
            }
        }

        private void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                T instance = Create();
                instance.gameObject.SetActive(false);
                _idle.Enqueue(instance);
            }
        }

        private T Create() => UnityEngine.Object.Instantiate(_prefab, _parent);
    }
}
