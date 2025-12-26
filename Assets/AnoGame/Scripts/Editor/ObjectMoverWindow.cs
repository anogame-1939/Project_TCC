using UnityEngine;
using UnityEditor;

namespace AnoGame.Scripts.Editor
{
    public class ObjectMoverWindow : EditorWindow
    {
        private bool _isActive = false;
        private float _moveSpeed = 0.5f;
        private bool _useCollision = false;
        private float _collisionBuffer = 0.1f;
        private LayerMask _collisionMask = -1; // Default to Everything

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
        }

        private void OnGUI()
        {
            GUILayout.Label("WASD Object Mover", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _isActive = EditorGUILayout.Toggle("Active", _isActive);
            if (EditorGUI.EndChangeCheck())
            {
                // Force scene repaint so we can see immediate feedback if we draw handles or similar
                SceneView.RepaintAll();
            }

            if (!_isActive)
            {
                EditorGUILayout.HelpBox("Check 'Active' to enable WASD controls for the selected object.", MessageType.Info);
                return;
            }

            _moveSpeed = EditorGUILayout.Slider("Speed", _moveSpeed, 0.1f, 10f);

            EditorGUILayout.Space();
            GUILayout.Label("Collision Settings", EditorStyles.boldLabel);
            _useCollision = EditorGUILayout.Toggle("Enable Collision", _useCollision);
            if (_useCollision)
            {
                _collisionBuffer = EditorGUILayout.FloatField("Buffer Distance", _collisionBuffer);
                // LayerMaskField is a bit tricky in custom windows sometimes, but this standard one works
                /* LayerMask is an int, but LayerMaskField expects LayerMask struct or int usage */
                int tempMask = EditorGUILayout.MaskField("Collision Layers", UnityEditorInternal.InternalEditorUtility.LayerMaskToConcatenatedLayersMask(_collisionMask), UnityEditorInternal.InternalEditorUtility.layers);
                _collisionMask = UnityEditorInternal.InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);
            }

            EditorGUILayout.Space();
            GUILayout.Label("Controls:", EditorStyles.miniLabel);
            GUILayout.Label("W / S : Forward / Backward (Camera relative)", EditorStyles.miniLabel);
            GUILayout.Label("A / D : Left / Right (Camera relative)", EditorStyles.miniLabel);
            GUILayout.Label("Q / E : Up / Down (World Up)", EditorStyles.miniLabel);
            GUILayout.Label("Shift : 3x Speed", EditorStyles.miniLabel);
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_isActive || Selection.activeTransform == null)
                return;

            Event e = Event.current;

            // Only process KeyDown to actually move? 
            // Better to process Layout/Repaint for smooth interaction, or just use KeyDown for discrete steps?
            // For smooth movement like a game, we usually need to check Key states.
            // But in Editor default 'KeyDown' events repeat after a delay. 
            // Let's try handling standard keys.

            if (e.isKey && (e.type == EventType.KeyDown || e.type == EventType.KeyUp))
            {
                // We consume keys if active to prevent other editor actions
                // List of keys we care about
                if (e.keyCode == KeyCode.W || e.keyCode == KeyCode.S ||
                    e.keyCode == KeyCode.A || e.keyCode == KeyCode.D ||
                    e.keyCode == KeyCode.Q || e.keyCode == KeyCode.E)
                {
                    // Do nothing here, just letting logic below run if it was KeyDown
                    // Actually for continuous movement we want to check input every frame?
                    // The SceneView doesn't update every frame unless we force it or mouse moves.
                    // If we want smooth movement, we need to RequestRepaint on KeyDown.
                }
            }

            if (e.type == EventType.KeyDown)
            {
                Vector3 moveDir = Vector3.zero;
                Camera cam = sceneView.camera;

                // Camera Forward/Right projected on flat plane roughly? 
                // Or true camera relative? Typically "Editor flying" is camera relative.
                Vector3 forward = cam.transform.forward;
                Vector3 right = cam.transform.right;

                // If user wants flat movement (XZ), we could project. 
                // Let's stick to true camera relative for now as it feels more intuitive for "flying" an object.
                // Or maybe flatten Y for WASD and use QE for UP/DOWN? 
                // Flattening Y is usually better for placing objects on floors.
                forward.y = 0;
                right.y = 0;
                forward.Normalize();
                right.Normalize();

                switch (e.keyCode)
                {
                    case KeyCode.W: moveDir += forward; break;
                    case KeyCode.S: moveDir -= forward; break;
                    case KeyCode.A: moveDir -= right; break;
                    case KeyCode.D: moveDir += right; break;
                    case KeyCode.Q: moveDir += Vector3.up; break;
                    case KeyCode.E: moveDir += Vector3.down; break;
                }

                if (moveDir != Vector3.zero)
                {
                    float speed = _moveSpeed;
                    if (e.shift) speed *= 3f;

                    Vector3 delta = moveDir.normalized * speed;
                    Transform target = Selection.activeTransform;

                    // Compute potential new position
                    Vector3 startPos = target.position;
                    Vector3 endPos = startPos + delta;

                    bool canMove = true;
                    if (_useCollision)
                    {
                        canMove = CheckCollision(target, startPos, delta);
                    }

                    if (canMove)
                    {
                        Undo.RecordObject(target, "Move Object (WASD)");
                        target.position = endPos;
                        e.Use(); // Consume the event so the view doesn't pan etc.
                    }
                }
            }
        }

        private bool CheckCollision(Transform target, Vector3 startPos, Vector3 delta)
        {
            // Simple bound box check or Raycast?
            // If the object has a collider, we can use it.
            Collider col = target.GetComponent<Collider>();

            float dist = delta.magnitude + _collisionBuffer;
            Vector3 dir = delta.normalized;

            if (col != null)
            {
                // Can't sweep the collider itself easily against others without RigidbodySweep usually, 
                // or Physics.BoxCast using bounds.
                // Physics.BoxCast is good.
                Vector3 center = col.bounds.center; // This is world space center
                Vector3 size = col.bounds.extents;  // half size

                // We need to offset center relative to pivot logic if we want to be precise, 
                // but bounds.center is current. We want to cast FROM current.

                // Note: BoxCast non-alloc is better but for editor tool minimal alloc is fine.
                // We must ignore the object itself!
                // Best way: temporary layer change or QueryTriggerInteraction?
                // BoxCastAll and check if hit is not self.

                RaycastHit[] hits = Physics.BoxCastAll(center, size, dir, target.rotation, dist, _collisionMask);
                foreach (var hit in hits)
                {
                    if (hit.transform != target && !hit.transform.IsChildOf(target))
                    {
                        // Hit something valid
                        return false;
                    }
                }
            }
            else
            {
                // Fallback to raycast if no collider
                if (Physics.Raycast(startPos, dir, dist, _collisionMask))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
