using UnityEngine;
using UnityEditor;

namespace AnoGame.Scripts.Editor
{
    public class ObjectMoverWindow : EditorWindow
    {
        private bool _isActive = false;
        private bool _isPlayerMode = false; // New: Local space movement
        private float _forwardOffset = 0f; // New: Rotation offset
        private float _moveSpeed = 0.5f;
        private bool _useCollision = false;
        private float _collisionBuffer = 0.1f;
        private LayerMask _collisionMask = -1;

        // Track pressed keys for smooth/diagonal movement
        private readonly System.Collections.Generic.HashSet<KeyCode> _pressedKeys = new System.Collections.Generic.HashSet<KeyCode>();

        [MenuItem("Tools/Object Mover")]
        public static void ShowWindow()
        {
            GetWindow<ObjectMoverWindow>("Object Mover");
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            _pressedKeys.Clear();
        }

        private void OnGUI()
        {
            GUILayout.Label("WASD Object Mover", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _isActive = EditorGUILayout.Toggle("Active", _isActive);
            if (EditorGUI.EndChangeCheck())
            {
                _pressedKeys.Clear(); // Reset keys when toggling
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
            GUILayout.Label("W / S : Forward / Backward", EditorStyles.miniLabel);
            GUILayout.Label("A / D : Left / Right", EditorStyles.miniLabel);
            GUILayout.Label("Q / E : Up / Down (World)", EditorStyles.miniLabel);
            GUILayout.Label("Shift : 3x Speed", EditorStyles.miniLabel);
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_isActive || Selection.activeTransform == null)
                return;

            Event e = Event.current;

            // Handle Key Events to update state
            if (e.type == EventType.KeyDown)
            {
                if (IsMoveKey(e.keyCode))
                {
                    if (_pressedKeys.Add(e.keyCode))
                    {
                        e.Use();
                    }
                }
            }
            else if (e.type == EventType.KeyUp)
            {
                if (IsMoveKey(e.keyCode))
                {
                    if (_pressedKeys.Remove(e.keyCode))
                    {
                        e.Use();
                    }
                }
            }

            // Execute movement if keys are pressed
            // We use generic event or repaint to drive the "update" loop in SceneView
            if (_pressedKeys.Count > 0)
            {
                MoveObject(sceneView.camera);

                // Force continuous updates while moving
                sceneView.Repaint();
            }
        }

        private bool IsMoveKey(KeyCode k)
        {
            return k == KeyCode.W || k == KeyCode.S ||
                   k == KeyCode.A || k == KeyCode.D ||
                   k == KeyCode.Q || k == KeyCode.E;
        }

        private void MoveObject(Camera cam)
        {
            Vector3 moveDir = Vector3.zero;
            Vector3 forward, right;

            if (_isPlayerMode && Selection.activeTransform != null)
            {
                // Local space relative to object
                // Apply rotation offset
                Quaternion offset = Quaternion.Euler(0, _forwardOffset, 0);
                forward = offset * Selection.activeTransform.forward;
                right = offset * Selection.activeTransform.right;
                // Typically we don't flatten Y for genuine local movement (like spaceship),
                // but for "Character" movement usually we move on XZ plane.
                // User asked for "Forward vector projected to keys", implies local direction.
                // Let's use full forward for now, it's safer for general "Object Mover".
                // Wait, user said "WASD... W/S: Front/Back... A/D: Left/Right... based on the set object vector".
                // I will use Transform.Forward and Transform.Right directly.
            }
            else
            {
                // Camera Relative
                forward = cam.transform.forward;
                right = cam.transform.right;
                forward.y = 0;
                right.y = 0;
                forward.Normalize();
                right.Normalize();
            }

            if (_pressedKeys.Contains(KeyCode.W)) moveDir += forward;
            if (_pressedKeys.Contains(KeyCode.S)) moveDir -= forward;
            if (_pressedKeys.Contains(KeyCode.D)) moveDir += right;
            if (_pressedKeys.Contains(KeyCode.A)) moveDir -= right;
            if (_pressedKeys.Contains(KeyCode.Q)) moveDir += Vector3.up; // Always World Up for convenience? Or Local Up? let's stick to World Up for Q/E as "Elevation"
            if (_pressedKeys.Contains(KeyCode.E)) moveDir += Vector3.down;

            if (moveDir == Vector3.zero) return;

            moveDir.Normalize();

            float speed = _moveSpeed;
            if (Event.current.shift) speed *= 3f;

            // Time.deltaTime doesn't exist reliably in Editor OnSceneGUI context like PlayMode.
            // We can use calculated delta time or fixed step.
            // Since Repaint() is called, it depends on refresh rate.
            // Let's use a small fixed multiplier or try to estimate.
            // approx 0.02f (60fps) is a safe bet for "per tick" feel,
            // or we use LastEditorTime.
            float dt = 0.02f;

            Vector3 delta = moveDir * (speed * dt); // Scale speed significantly since it's per-frame-ish

            // Speed factor needs to be higher if we use 0.02, previously it was discrete keydown.
            // Previous code: delta = moveDir * speed. (One big step per KeyDown).
            // Now continuous: speed * 0.02.
            // To keep "Speed = 1.0" feeling similar, we might need to boost the multiplier.
            // Let's just use the speed value directly but realize it's per-tick now.
            // A slider 0.1 to 10 is fine.

            Transform target = Selection.activeTransform;
            Vector3 startPos = target.position;
            Vector3 endPos = startPos + delta;

            if (_useCollision)
            {
                if (!CheckCollision(target, startPos, delta))
                {
                    return; // Bloced
                }
            }

            Undo.RecordObject(target, "Move Object");
            target.position = endPos;
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
