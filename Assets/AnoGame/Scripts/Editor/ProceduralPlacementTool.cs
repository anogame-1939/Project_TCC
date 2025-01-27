using UnityEngine;
using UnityEditor;

namespace AnoGame.EditorExtensions
{
    public class ProceduralPlacementTool : EditorWindow
    {
        private GameObject prefab;
        private int count = 10;
        private float radius = 5f;
        private float minScale = 0.8f;
        private float maxScale = 1.2f;
        private float yOffset = 0f;
        private bool randomRotation = true;
        private bool alignToSurface = true;
        private LayerMask surfaceLayer = -1;

        [MenuItem("Tools/Procedural Placement Tool")]
        public static void ShowWindow()
        {
            GetWindow<ProceduralPlacementTool>("Procedural Placement");
        }

        private void OnGUI()
        {
            GUILayout.Label("Procedural Placement Settings", EditorStyles.boldLabel);

            prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false);
            count = EditorGUILayout.IntField("Count", count);
            radius = EditorGUILayout.FloatField("Radius", radius);
            
            EditorGUILayout.Space();
            
            GUILayout.Label("Scale Settings", EditorStyles.boldLabel);
            minScale = EditorGUILayout.FloatField("Min Scale", minScale);
            maxScale = EditorGUILayout.FloatField("Max Scale", maxScale);
            
            EditorGUILayout.Space();
            
            GUILayout.Label("Placement Settings", EditorStyles.boldLabel);
            yOffset = EditorGUILayout.FloatField("Y Offset", yOffset);
            randomRotation = EditorGUILayout.Toggle("Random Rotation", randomRotation);
            alignToSurface = EditorGUILayout.Toggle("Align to Surface", alignToSurface);
            surfaceLayer = EditorGUILayout.LayerField("Surface Layer", surfaceLayer);

            EditorGUILayout.Space();

            GUI.enabled = prefab != null;
            if (GUILayout.Button("Generate"))
            {
                GenerateObjects();
            }
            if (GUILayout.Button("Clear Generated"))
            {
                ClearGenerated();
            }
            GUI.enabled = true;
        }

        private void GenerateObjects()
        {
            if (prefab == null) return;

            GameObject container = new GameObject("Generated_Objects");
            Undo.RegisterCreatedObjectUndo(container, "Generate Objects");

            for (int i = 0; i < count; i++)
            {
                // Generate random position within radius
                Vector2 randomCircle = Random.insideUnitCircle * radius;
                Vector3 position = new Vector3(randomCircle.x, 0, randomCircle.y);

                // Raycast to find surface
                if (alignToSurface)
                {
                    RaycastHit hit;
                    Vector3 rayStart = position + Vector3.up * 1000f;
                    if (Physics.Raycast(rayStart, Vector3.down, out hit, Mathf.Infinity, surfaceLayer))
                    {
                        position = hit.point;
                        // デバッグ用の視覚化（エディタでのみ表示）
                        Debug.DrawLine(rayStart, hit.point, Color.green, 2f);
                    }
                    else
                    {
                        // Raycastが失敗した場合はy=0に設定
                        position.y = 0f;
                        Debug.DrawLine(rayStart, position, Color.red, 2f);
                        Debug.LogWarning($"Raycast failed at position {position}. Setting y to 0.");
                    }
                }
                else
                {
                    position.y = 0f;
                }

                // オフセットを適用
                position.y += yOffset;

                // Create object
                GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                obj.transform.position = position;
                obj.transform.SetParent(container.transform);

                // Random rotation
                if (randomRotation)
                {
                    obj.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                }

                // Random scale
                float scale = Random.Range(minScale, maxScale);
                obj.transform.localScale = Vector3.one * scale;

                Undo.RegisterCreatedObjectUndo(obj, "Generate Object");
            }
        }

        private void ClearGenerated()
        {
            GameObject[] generated = GameObject.FindGameObjectsWithTag("Generated_Objects");
            foreach (GameObject obj in generated)
            {
                Undo.DestroyObjectImmediate(obj);
            }
        }
    }
}