using UnityEngine;
using UnityEditor;
using System.Runtime.InteropServices;

namespace AnoGame.Scripts.Editor
{
    public class ObjectMoverWindow : EditorWindow
    {
        private bool _isActive = false;
        private bool _isPlayerMode = false;
        private float _forwardOffset = 0f;
        private float _moveSpeed = 0.5f;
        private bool _useCollision = false;
        private float _collisionBuffer = 0.1f;
        private LayerMask _collisionMask = -1;

        // Windows API for global key state
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private const int VK_W = 0x57;
        private const int VK_A = 0x41;
        private const int VK_S = 0x53;
        private const int VK_D = 0x44;
        private const int VK_Q = 0x51;
        private const int VK_E = 0x45;
        private const int VK_SHIFT = 0x10;

        private double _lastUpdateTime = 0;

        [MenuItem("Tools/Object Mover")]
        public static void ShowWindow()
        {
            GetWindow<ObjectMoverWindow>("Object Mover");
        }

        private void OnEnable()
        {
            // We use EditorApplication.update to polling input globally
            EditorApplication.update += OnEditorUpdate;
            _lastUpdateTime = EditorApplication.timeSinceStartup;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnGUI()
        {
            GUILayout.Label("WASD Object Mover (Global)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Works in Scene View AND Game View (Edit Mode)", EditorStyles.miniLabel);

            EditorGUI.BeginChangeCheck();
            _isActive = EditorGUILayout.Toggle("Active", _isActive);
            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }

            if (!_isActive)
            {
                EditorGUILayout.HelpBox("Check 'Active' to enable WASD controls.", MessageType.Info);
                return;
            }

            _isPlayerMode = EditorGUILayout.Toggle("Is Player (Local)", _isPlayerMode);
            if (_isPlayerMode)
            {
                _forwardOffset = EditorGUILayout.Slider("Front Offset (Y)", _forwardOffset, 0f, 360f);
            }

            _moveSpeed = EditorGUILayout.Slider("Speed", _moveSpeed, 0.1f, 10f);

            EditorGUILayout.Space();
            GUILayout.Label("Collision Settings", EditorStyles.boldLabel);
            _useCollision = EditorGUILayout.Toggle("Enable Collision", _useCollision);
            if (_useCollision)
            {
                _collisionBuffer = EditorGUILayout.FloatField("Buffer Distance", _collisionBuffer);
                int tempMask = EditorGUILayout.MaskField("Collision Layers", UnityEditorInternal.InternalEditorUtility.LayerMaskToConcatenatedLayersMask(_collisionMask), UnityEditorInternal.InternalEditorUtility.layers);
                _collisionMask = UnityEditorInternal.InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);
            }

            EditorGUILayout.Space();
            GUILayout.Label("Controls:", EditorStyles.miniLabel);
            GUILayout.Label("W/A/S/D : Move", EditorStyles.miniLabel);
            GUILayout.Label("Q / E : Up / Down", EditorStyles.miniLabel);
            GUILayout.Label("Shift : Fast", EditorStyles.miniLabel);
        }

        private void OnEditorUpdate()
        {
            if (!_isActive || Selection.activeTransform == null) return;

            // Only update if Scene View or Game View has focus to avoid accidents
            EditorWindow focused = EditorWindow.focusedWindow;
            if (focused == null) return;

            // "SceneView" and "PlayModeView" (Game View) are what we care about.
            string winType = focused.GetType().Name;

            // Debug.Log(winType); 
            // Standard names: "SceneView", "GameView" (may vary in versions, usually GameView)

            bool isScene = winType == "SceneView";
            bool isGame = winType == "GameView" || winType == "PlayModeView"; // some versions use PlayModeView?

            // Actually, in newer Unity versions it is often UnityEditor.GameView
            // Simple check:
            if (!isScene && !focused.titleContent.text.Contains("Game") && !focused.titleContent.text.Contains("Scene"))
            {
                // Safety: If not focused on Scene or Game, don't move.
                return;
            }

            double currentTime = EditorApplication.timeSinceStartup;
            float dt = (float)(currentTime - _lastUpdateTime);
            _lastUpdateTime = currentTime;

            // Cap dt to prevent huge jumps after lag/compile
            if (dt > 0.1f) dt = 0.1f;

            // Check Keys using Windows API
            // GetAsyncKeyState returns short. High bit set means pressed.
            bool w = (GetAsyncKeyState(VK_W) & 0x8000) != 0;
            bool a = (GetAsyncKeyState(VK_A) & 0x8000) != 0;
            bool s = (GetAsyncKeyState(VK_S) & 0x8000) != 0;
            bool d = (GetAsyncKeyState(VK_D) & 0x8000) != 0;
            bool q = (GetAsyncKeyState(VK_Q) & 0x8000) != 0;
            bool e = (GetAsyncKeyState(VK_E) & 0x8000) != 0;
            bool shift = (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;

            if (!w && !a && !s && !d && !q && !e) return;

            // Logic similar to before
            MoveObject(dt, w, s, a, d, q, e, shift);

            // Force repaint if in scene view to see update smooth
            if (isScene)
            {
                // Repaint focused SceneView
                (focused as SceneView)?.Repaint();
            }
        }

        private void MoveObject(float dt, bool w, bool s, bool a, bool d, bool q, bool e, bool shift)
        {
            Vector3 moveDir = Vector3.zero;
            Transform target = Selection.activeTransform;

            Vector3 forward, right;

            if (_isPlayerMode)
            {
                Quaternion offset = Quaternion.Euler(0, _forwardOffset, 0);
                forward = offset * target.forward;
                right = offset * target.right;
            }
            else
            {
                // Camera Relative
                // Which camera?
                // If in SceneView, use scene camera. 
                // If in GameView, use Main Camera.

                Camera refCam = null;
                if (EditorWindow.focusedWindow.GetType().Name == "SceneView")
                {
                    refCam = SceneView.lastActiveSceneView.camera;
                }
                else
                {
                    refCam = Camera.main; // Game View main camera if valid
                }

                if (refCam != null)
                {
                    forward = refCam.transform.forward;
                    right = refCam.transform.right;
                    forward.y = 0;
                    right.y = 0;
                    forward.Normalize();
                    right.Normalize();
                }
                else
                {
                    forward = Vector3.forward;
                    right = Vector3.right;
                }
            }

            if (w) moveDir += forward;
            if (s) moveDir -= forward;
            if (d) moveDir += right;
            if (a) moveDir -= right;
            if (q) moveDir += Vector3.up;
            if (e) moveDir += Vector3.down;

            if (moveDir == Vector3.zero) return;

            moveDir.Normalize();
            float speed = _moveSpeed * (shift ? 3f : 1f);
            Vector3 delta = moveDir * (speed * dt);

            if (_useCollision)
            {
                if (!CheckCollision(target, target.position, delta)) return;
            }

            Undo.RecordObject(target, "Move Object");
            target.position += delta;
        }

        private bool CheckCollision(Transform target, Vector3 startPos, Vector3 delta)
        {
            Collider col = target.GetComponent<Collider>();
            float dist = delta.magnitude + _collisionBuffer;
            Vector3 dir = delta.normalized;

            if (col != null)
            {
                Vector3 center = col.bounds.center;
                Vector3 size = col.bounds.extents;
                RaycastHit[] hits = Physics.BoxCastAll(center, size, dir, target.rotation, dist, _collisionMask);
                foreach (var hit in hits)
                {
                    if (hit.transform != target && !hit.transform.IsChildOf(target))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (Physics.Raycast(startPos, dir, dist, _collisionMask))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
