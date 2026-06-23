using System.Collections.Generic;

using UnityEngine;

namespace SplitRun.Obstacle
{
    // Object pool for a single obstacle prefab variant. ObstacleSpawner creates one pool
    // per non-null prefab. Each obstacle (single or composite coop) carries exactly one
    // root TrackObstacle, so Rent() restores it directly with no child lookup.
    public class ObstaclePool
    {
        private readonly TrackObstacle _prefab;
        private readonly Transform     _parent;
        private readonly Queue<TrackObstacle> _idle = new Queue<TrackObstacle>();

        public ObstaclePool(TrackObstacle prefab, Transform parent, int initialSize)
        {
            _prefab = prefab;
            _parent = parent;

            Prewarm(initialSize);
        }

        // Returns an idle instance (or creates one when the idle queue is empty) and
        // restores it. ResetState() re-activates the GameObject.
        public TrackObstacle Rent()
        {
            TrackObstacle instance = _idle.Count > 0 ? _idle.Dequeue() : CreateInstance();

            instance.ResetState();
            return instance;
        }

        // Deactivates the instance and re-queues it for future Rent() calls.
        public void Return(TrackObstacle instance)
        {
            instance.gameObject.SetActive(false);
            _idle.Enqueue(instance);
        }

        // Destroys all idle instances. Active instances are children of the spawner's
        // transform and are destroyed when the spawner GameObject is destroyed.
        public void Dispose()
        {
            while (_idle.Count > 0)
            {
                TrackObstacle instance = _idle.Dequeue();
                if (instance != null)
                    Object.Destroy(instance.gameObject);
            }
        }

        private void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                TrackObstacle instance = CreateInstance();
                instance.gameObject.SetActive(false);
                _idle.Enqueue(instance);
            }
        }

        private TrackObstacle CreateInstance() => Object.Instantiate(_prefab, _parent);
    }
}
