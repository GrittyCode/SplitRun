using UnityEditor;
using UnityEngine;

using SplitRun.Obstacle;

namespace SplitRun.EditorTools
{
    [CustomEditor(typeof(TrackObstacle))]
    public class TrackObstacleEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var obstacle = (TrackObstacle)target;
            if (obstacle.IsRootTransformValid()) return;

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Root Transform is not identity. Move all visual scale/rotation to the child " +
                "Model. The root must stay Scale (1,1,1) / Rotation (0,0,0) so the stamped " +
                "collider holds its gameplay-authoritative size in world space.",
                MessageType.Error);
        }
    }
}
