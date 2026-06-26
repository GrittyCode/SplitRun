using UnityEditor;
using UnityEngine;

using SplitRun.Environment;

namespace SplitRun.EditorTools
{
    [CustomEditor(typeof(TrackSegment))]
    public class TrackSegmentEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var segment = (TrackSegment)target;

            if (!segment.Floor)
            {
                Show("Assign the Floor child — it defines the tile length.", MessageType.Error);
                return;
            }

            if (!segment.TryGetFloorMetrics(out _, out _))
                Show("Floor has no mesh yet.", MessageType.Info);

            if (!segment.IsRootTransformValid())
                Show("Root must be identity (Pos 0 / Rot 0 / Scale 1). Move offsets to children.", MessageType.Error);

            if (segment.HasDecorationOverhang())
                Show("Decoration overhangs the floor tile (overlaps the next tile).", MessageType.Warning);
        }

        private static void Show(string message, MessageType type)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(message, type);
        }
    }
}
