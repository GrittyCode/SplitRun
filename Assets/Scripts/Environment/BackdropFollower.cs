using UnityEngine;

using SplitRun.Character;
using SplitRun.Constants;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SplitRun.Environment
{
    // Rides the character's Z so the backdrop holds a fixed distance from the (Z-tracking)
    // camera. Rotation stays identity so camera-pitch tuning never tilts the silhouette.
    public class BackdropFollower : MonoBehaviour
    {
        [Tooltip("Forward gap from the character to the silhouette group. Far beyond the ground " +
                 "fill distance so it reads as background, not approaching geometry.")]
        [SerializeField] private float _forwardOffsetZ = 125f;

        [Tooltip("Runtime vertical position of the silhouette group. Drag this against the live " +
                 "game view until the silhouette sits on the horizon — the Scene gizmo slides the " +
                 "camera frame as you change it.")]
        [SerializeField] private float _baselineY = -33f;

        private Transform _target;

        public bool IsRootRotationValid() =>
            Quaternion.Angle(transform.localRotation, Quaternion.identity) < 0.001f;

        private void OnEnable()
        {
            CharacterEvents.OnSpawned   += OnCharacterSpawned;
            CharacterEvents.OnDespawned += OnCharacterDespawned;
        }

        private void OnDisable()
        {
            CharacterEvents.OnSpawned   -= OnCharacterSpawned;
            CharacterEvents.OnDespawned -= OnCharacterDespawned;
        }

        private void LateUpdate()
        {
            if (!_target) return;

            transform.position = new Vector3(
                0f,
                _baselineY,
                _target.position.z + _forwardOffsetZ
            );
        }

        private void OnCharacterSpawned(ICharacter character) => _target = character.CharacterTransform;

        private void OnCharacterDespawned(ICharacter character)
        {
            if (_target == character.CharacterTransform) _target = null;
        }

#if UNITY_EDITOR
        // Fixed 9:16 mobile portrait — the build's target aspect, used only to size the framing
        // gizmo's width.
        private const float k_GizmoAspect = 9f / 16f;

        // Reconstructs the run-start camera (characterZ = 0) and projects its view onto the
        // silhouette plane. Baseline Y is the single control: changing it moves the camera relative
        // to the silhouette here, so the frame slides over the meshes exactly as it will in-game.
        //   cyan   — run-start camera frame (fill the silhouette across this width)
        //   green  — Baseline Y plane (align this with the live game horizon)
        //   yellow — the silhouette meshes currently authored
        private void OnDrawGizmos()
        {
            if (!IsThisHierarchySelected()) return;
            if (!TryComputeFrame(out Vector3 camLocal, out Vector3[] corners)) return;

            Gizmos.matrix  = transform.localToWorldMatrix;
            Handles.matrix = transform.localToWorldMatrix;

            DrawCameraFrame(camLocal, corners);
            DrawBaselinePlane(corners);
            DrawCurrentSilhouette();
        }

        // Intersects the four camera corner rays with the silhouette plane (local Z = 0). The plane
        // sits at the root origin, so corners come back in the same local space the author edits in.
        private bool TryComputeFrame(out Vector3 camLocal, out Vector3[] corners)
        {
            camLocal = new Vector3(
                0f,
                CameraConstants.k_CameraOffsetY - _baselineY,
                CameraConstants.k_CameraOffsetZ - _forwardOffsetZ);
            corners = new Vector3[4];

            Quaternion camRot = Quaternion.Euler(CameraConstants.k_CameraPitchAngle, 0f, 0f);
            float tanV = Mathf.Tan(CameraConstants.k_CameraFov * 0.5f * Mathf.Deg2Rad);
            float tanH = tanV * k_GizmoAspect;

            Vector3[] viewDirs =
            {
                new Vector3(-tanH,  tanV, 1f),   // top-left
                new Vector3( tanH,  tanV, 1f),   // top-right
                new Vector3( tanH, -tanV, 1f),   // bottom-right
                new Vector3(-tanH, -tanV, 1f),   // bottom-left
            };

            for (int i = 0; i < 4; i++)
            {
                Vector3 dir = camRot * viewDirs[i];
                if (dir.z <= 0.0001f) return false;

                corners[i] = camLocal + dir * (-camLocal.z / dir.z);
            }

            return true;
        }

        private void DrawCameraFrame(Vector3 camLocal, Vector3[] corners)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
            Gizmos.DrawWireSphere(camLocal, 1f);

            for (int i = 0; i < 4; i++)
            {
                Gizmos.DrawLine(camLocal, corners[i]);
                Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
            }

            Handles.color = new Color(0f, 1f, 1f, 0.7f);
            Handles.Label(Vector3.Lerp(corners[0], corners[1], 0.5f), "  Camera view — fill silhouette across this width");
        }

        // The Baseline Y plane is local Y = 0 (the root origin equals the runtime Baseline Y height).
        private void DrawBaselinePlane(Vector3[] corners)
        {
            if (!TryEdgeXAtHeight(corners[3], corners[0], 0f, out float leftX))  return;
            if (!TryEdgeXAtHeight(corners[2], corners[1], 0f, out float rightX)) return;

            Gizmos.color = Color.green;
            Gizmos.DrawLine(new Vector3(leftX, 0f, 0f), new Vector3(rightX, 0f, 0f));

            Handles.color = Color.green;
            Handles.Label(new Vector3(leftX, 0f, 0f), "  Baseline Y — align with live game horizon");
        }

        private void DrawCurrentSilhouette()
        {
            if (!TryGetSilhouetteLocalBounds(out Bounds local)) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(new Vector3(local.center.x, local.center.y, 0f),
                                new Vector3(local.size.x, local.size.y, 0f));

            Handles.color = Color.yellow;
            Handles.Label(new Vector3(local.center.x, local.max.y, 0f), "  Current silhouette");
        }

        private bool TryGetSilhouetteLocalBounds(out Bounds local)
        {
            local = default;

            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return false;

            Bounds world = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                world.Encapsulate(renderers[i].bounds);

            local = new Bounds(transform.InverseTransformPoint(world.center), world.size);
            return true;
        }

        private static bool TryEdgeXAtHeight(Vector3 a, Vector3 b, float y, out float x)
        {
            x = 0f;

            float dy = b.y - a.y;
            if (Mathf.Abs(dy) < 0.0001f) return false;

            float t = (y - a.y) / dy;
            if (t < 0f || t > 1f) return false;

            x = Mathf.Lerp(a.x, b.x, t);
            return true;
        }

        private bool IsThisHierarchySelected()
        {
            GameObject active = Selection.activeGameObject;
            if (!active) return false;

            return active == gameObject || active.transform.IsChildOf(transform);
        }
#endif
    }
}
