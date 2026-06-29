using System.Collections.Generic;

using UnityEngine;

namespace SplitRun.Obstacle
{
    public class ObstaclePool
    {
        private readonly TrackObstacle       _prefab;
        private readonly Transform           _parent;
        private readonly Queue<TrackObstacle> _idle = new Queue<TrackObstacle>();

        public ObstaclePool(TrackObstacle prefab, Transform parent, int initialSize)
        {
            _prefab = prefab;
            _parent = parent;

            Prewarm(initialSize);
        }

        public TrackObstacle Rent()
        {
            TrackObstacle instance = _idle.Count > 0 ? _idle.Dequeue() : CreateInstance();
            instance.ResetState();
            return instance;
        }

        public void Return(TrackObstacle instance)
        {
            instance.gameObject.SetActive(false);
            _idle.Enqueue(instance);
        }

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
