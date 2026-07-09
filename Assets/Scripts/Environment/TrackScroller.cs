using System.Collections.Generic;

using UnityEngine;

using R3;
using VContainer;

using SplitRun.Constants;
using SplitRun.Game;
using SplitRun.Utility;

namespace SplitRun.Environment
{
    // Cosmetic endless ground — tiles the theme segment ahead, recycles behind; no gameplay rules.
    public class TrackScroller : MonoBehaviour
    {
        [Inject] private GameService       _gameService;
        [Inject] private WorldThemeProfile _theme;

        private readonly Queue<ActiveSegment> _active = new Queue<ActiveSegment>();
        private readonly Queue<Transform>     _idle   = new Queue<Transform>();

        private Transform _segmentPrefab;
        private float     _segmentLengthZ;
        private float     _segmentMinZ;
        private float     _nextSegmentZ;

        private void Start()
        {
            if (!_theme || !_theme.SegmentPrefab)
            {
                Debug.LogWarning("[TrackScroller] No theme segment prefab assigned — track disabled.");
                return;
            }

            _segmentPrefab = _theme.SegmentPrefab;

            if (!TryMeasureSegment()) return;

            // Ground is laid during the intro (when there is one) so it is visible before the run begins.
            _gameService.Phase
                .Where(phase => phase == GamePhase.Intro || phase == GamePhase.Running)
                .Take(1)
                .Subscribe(_ => OnRunStarted())
                .AddTo(this);

            _gameService.CurrentDistance
                .Skip(1)
                .Subscribe(OnDistanceChanged)
                .AddTo(this);
        }

        private bool TryMeasureSegment()
        {
            Transform probe = Instantiate(_segmentPrefab, transform);
            probe.position  = Vector3.zero;

            bool measured = TryMeasureFloor(probe, out float lengthZ, out float minZ)
                         || TryMeasureRenderers(probe, out lengthZ, out minZ);

            Destroy(probe.gameObject);

            if (!measured || lengthZ <= 0f)
            {
                Debug.LogError("[TrackScroller] Segment prefab has no measurable floor or renderers — track disabled.");
                return false;
            }

            _segmentLengthZ = lengthZ;
            _segmentMinZ    = minZ;
            return true;
        }

        private static bool TryMeasureFloor(Transform probe, out float lengthZ, out float minZ)
        {
            lengthZ = 0f;
            minZ    = 0f;

            if (!probe.TryGetComponent(out TrackSegment segment)) return false;

            return segment.TryGetFloorMetrics(out lengthZ, out minZ);
        }

        private static bool TryMeasureRenderers(Transform root, out float lengthZ, out float minZ)
        {
            lengthZ = 0f;
            minZ    = 0f;

            if (!GeometryUtils.TryGetHierarchyBounds(root, out Bounds bounds)) return false;

            lengthZ = bounds.size.z;
            minZ    = bounds.min.z;
            return true;
        }

        private void OnRunStarted()
        {
            _nextSegmentZ = SnapToBoundary(-TrackConstants.k_TrackRecycleBehindDistance);
            FillAhead(0f);
        }

        private void OnDistanceChanged(float characterZ)
        {
            RecycleBehind(characterZ);
            FillAhead(characterZ);
        }

        private void FillAhead(float characterZ)
        {
            float frontier = characterZ + TrackConstants.k_TrackFillAheadDistance;

            while (_nextSegmentZ < frontier)
            {
                PlaceSegment(_nextSegmentZ);
                _nextSegmentZ += _segmentLengthZ;
            }
        }

        private void RecycleBehind(float characterZ)
        {
            float threshold = characterZ - TrackConstants.k_TrackRecycleBehindDistance;

            while (_active.Count > 0 && _active.Peek().NearZ + _segmentLengthZ < threshold)
            {
                ActiveSegment segment = _active.Dequeue();
                segment.Instance.gameObject.SetActive(false);
                _idle.Enqueue(segment.Instance);
            }
        }

        private void PlaceSegment(float nearZ)
        {
            // Offset by the measured min so the mesh near edge lands on nearZ regardless of pivot.
            Transform segment = RentSegment();
            segment.position  = new Vector3(0f, 0f, nearZ - _segmentMinZ);
            _active.Enqueue(new ActiveSegment(segment, nearZ));
        }

        private Transform RentSegment()
        {
            Transform segment = _idle.Count > 0 ? _idle.Dequeue() : Instantiate(_segmentPrefab, transform);
            segment.gameObject.SetActive(true);
            return segment;
        }

        private float SnapToBoundary(float z) => Mathf.Floor(z / _segmentLengthZ) * _segmentLengthZ;

        private readonly struct ActiveSegment
        {
            public Transform Instance { get; }
            public float     NearZ    { get; }

            public ActiveSegment(Transform instance, float nearZ)
            {
                Instance = instance;
                NearZ    = nearZ;
            }
        }
    }
}
