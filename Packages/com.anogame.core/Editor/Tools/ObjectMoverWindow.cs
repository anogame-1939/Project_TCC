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

        private Transform _targetObject;
        private bool _lockY = false;
        private float _targetY = 0.5f;

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
                _targetObject = (Transform)EditorGUILayout.ObjectField("Target Player", _targetObject, typeof(Transform), true);
                _forwardOffset = EditorGUILayout.Slider("Front Offset (Y)", _forwardOffset, 0f, 360f);

                if (_targetObject == null)
                {
                    EditorGUILayout.HelpBox("Assign a Target Player transform to move.", MessageType.Warning);
                }

                EditorGUILayout.BeginHorizontal();
                _lockY = EditorGUILayout.Toggle("Lock Y Height", _lockY);
                if (_lockY)
                {
                    _targetY = EditorGUILayout.FloatField(_targetY);
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("Moves CURRENT SELECTION relative to Camera.", MessageType.None);
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
            if (!_isActive) return;

            Transform target = null;
            if (_isPlayerMode)
            {
                target = _targetObject;
            }
            else
            {
                target = Selection.activeTransform;
            }

            if (target == null) return;

            // Only update if Scene View or Game View has focus to avoid key spam while in other windows
            EditorWindow focused = EditorWindow.focusedWindow;
            if (focused == null) return;

            string winType = focused.GetType().Name;
            bool isScene = winType == "SceneView";
            bool isGame = winType == "GameView" || winType == "PlayModeView";

            if (!isScene && !focused.titleContent.text.Contains("Game") && !focused.titleContent.text.Contains("Scene"))
            {
                return;
            }

            double currentTime = EditorApplication.timeSinceStartup;
            float dt = (float)(currentTime - _lastUpdateTime);
            _lastUpdateTime = currentTime;

            if (dt > 0.1f) dt = 0.1f;

            bool w = (GetAsyncKeyState(VK_W) & 0x8000) != 0;
            bool a = (GetAsyncKeyState(VK_A) & 0x8000) != 0;
            bool s = (GetAsyncKeyState(VK_S) & 0x8000) != 0;
            bool d = (GetAsyncKeyState(VK_D) & 0x8000) != 0;
            bool q = (GetAsyncKeyState(VK_Q) & 0x8000) != 0;
            bool e = (GetAsyncKeyState(VK_E) & 0x8000) != 0;
            bool shift = (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;

            if (!w && !a && !s && !d && !q && !e) return; // If lockY is on, we might need to enforce Y even with no keys? Only on key press for now to save performance, or always? 
                                                          // If the user wants "Lock Y", they expect it to SNAP back if it drifts.
                                                          // But since this tool is driven by keys, let's only correct when keys are pressed OR just always check?
                                                          // "on Update" implies continuous enforcement.
                                                          // Let's enforce Y even if NO keys are pressed, IF Active.
                                                          // But checking keys allows us to "return" early.
                                                          // Since this is an "Object Mover", maybe only enforce when moving.

            // However, "Mystery Y value" drift might happen from other sources? No, prob only when moving.
            if (!w && !a && !s && !d && !q && !e)
            {
                // Just to be safe, if LockY is ON, we force it once?
                // Actually, if we just return here, we don't fix potential drift from last frame if it happened after.
                // Let's proceed if keys OR lockY is needed? No, drift happens during MoveObject.
                // So checking keys is fine.
                return;
            }

            MoveObject(target, dt, w, s, a, d, q, e, shift);

            if (isScene)
            {
                (focused as SceneView)?.Repaint();
            }
        }

        private void MoveObject(Transform target, float dt, bool w, bool s, bool a, bool d, bool q, bool e, bool shift)
        {
            Vector3 moveDir = Vector3.zero;
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
                Camera refCam = null;
                if (EditorWindow.focusedWindow.GetType().Name == "SceneView")
                {
                    refCam = SceneView.lastActiveSceneView.camera;
                }
                else
                {
                    refCam = Camera.main;
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
            if (q && !_lockY) moveDir += Vector3.up; // Disable Q/E if Locked
            if (e && !_lockY) moveDir += Vector3.down;

            if (moveDir != Vector3.zero)
            {
                moveDir.Normalize();
                float speed = _moveSpeed * (shift ? 3f : 1f);
                Vector3 delta = moveDir * (speed * dt);

                if (_useCollision)
                {
                    Vector3 finalDelta = delta;
                    RaycastHit hitInfo;
                    if (CheckCollision(target, target.position, delta, out hitInfo))
                    {
                        Debug.Log($"[ObjectMover] Hit: {hitInfo.collider.name}");
                        Vector3 slide = Vector3.ProjectOnPlane(delta, hitInfo.normal);
                        finalDelta = slide;
                    }
                    target.position += finalDelta;
                }
                else
                {
                    target.position += delta;
                }
            }

            // Enforce Lock Y
            if (_isPlayerMode && _lockY)
            {
                Vector3 pos = target.position;
                if (Mathf.Abs(pos.y - _targetY) > 0.001f)
                {
                    pos.y = _targetY;
                    target.position = pos;
                }
            }

            Undo.RecordObject(target, "Move Object");
        }

        private bool CheckCollision(Transform target, Vector3 startPos, Vector3 delta, out RaycastHit hitInfo)
        {
            hitInfo = default;
            Vector3 flatDelta = new Vector3(delta.x, 0f, delta.z);

            if (flatDelta.sqrMagnitude < 0.00001f)
            {
                return false;
            }

            Collider col = target.GetComponent<Collider>();
            float dist = flatDelta.magnitude + _collisionBuffer;
            Vector3 dir = flatDelta.normalized;

            if (col != null)
            {
                Vector3 center = col.bounds.center;
                Vector3 size = col.bounds.extents;
                size.y *= 0.1f; // 10% height
                size.x *= 0.95f;
                size.z *= 0.95f;

                // VISUALIZATION
                DrawWireBox(center, size, target.rotation, Color.yellow, 0.1f);
                Debug.DrawRay(center, dir * dist, Color.red, 0.1f);

                RaycastHit[] hits = Physics.BoxCastAll(center, size, dir, target.rotation, dist, _collisionMask, QueryTriggerInteraction.Ignore);

                float minDist = float.MaxValue;
                bool found = false;

                foreach (var hit in hits)
                {
                    if (hit.transform != target && !hit.transform.IsChildOf(target))
                    {
                        if (hit.distance < minDist)
                        {
                            minDist = hit.distance;
                            hitInfo = hit;
                            found = true;
                        }
                    }
                }
                return found;
            }
            else
            {
                if (Physics.Raycast(startPos, dir, out hitInfo, dist, _collisionMask, QueryTriggerInteraction.Ignore))
                {
                    return true;
                }
            }
            return false;
        }

        private void DrawWireBox(Vector3 center, Vector3 extents, Quaternion rotation, Color color, float duration)
        {
            Vector3[] points = new Vector3[8];
            int i = 0;
            // Create 8 corners based on extents (local)
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 pt = new Vector3(x * extents.x, y * extents.y, z * extents.z);
                        points[i++] = center + rotation * pt;
                    }
                }
            }
            // Draw edges
            // Bottom ring
            Debug.DrawLine(points[0], points[2], color, duration); // -+- to +--
            Debug.DrawLine(points[2], points[3], color, duration);
            Debug.DrawLine(points[3], points[1], color, duration);
            Debug.DrawLine(points[1], points[0], color, duration);
            // Top ring
            Debug.DrawLine(points[4], points[6], color, duration);
            Debug.DrawLine(points[6], points[7], color, duration);
            Debug.DrawLine(points[7], points[5], color, duration);
            Debug.DrawLine(points[5], points[4], color, duration);
            // Vertical pillars
            Debug.DrawLine(points[0], points[4], color, duration);
            Debug.DrawLine(points[1], points[5], color, duration);
            Debug.DrawLine(points[2], points[6], color, duration);
            Debug.DrawLine(points[3], points[7], color, duration);
        }
    }
}
