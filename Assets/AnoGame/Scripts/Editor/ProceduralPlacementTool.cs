using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AnoGame.EditorExtensions
{
    public class ProceduralPlacementTool : EditorWindow
    {
        private GameObject prefab;
        
        private int count = 10;
        private float radius = 5f;
        
        private float densityMultiplier = 2f;
        private float minDistance = 1f;
        
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
            GUILayout.Label("Density Settings", EditorStyles.boldLabel);
            densityMultiplier = EditorGUILayout.Slider("Density Power", densityMultiplier, 0.1f, 5f);
            minDistance = EditorGUILayout.FloatField("Min Distance", minDistance);
            
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

        private List<Vector3> GeneratePositions()
        {
            List<Vector3> positions = new List<Vector3>();
            int maxAttempts = count * 10;
            int currentAttempts = 0;

            while (positions.Count < count && currentAttempts < maxAttempts)
            {
                // 極座標でランダムな位置を生成
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float distanceFromCenter = radius * Mathf.Sqrt(Random.Range(0f, 1f));
                
                // 中心からの距離に応じて配置確率を調整
                float normalizedDistance = distanceFromCenter / radius;
                float placementProbability = Mathf.Pow(normalizedDistance, densityMultiplier);
                
                if (Random.value < placementProbability)
                {
                    // 極座標からデカルト座標に変換
                    Vector3 position = new Vector3(
                        distanceFromCenter * Mathf.Cos(angle),
                        0,
                        distanceFromCenter * Mathf.Sin(angle)
                    );
                    
                    // 最小距離チェック
                    bool tooClose = false;
                    foreach (Vector3 existingPos in positions)
                    {
                        if (Vector3.Distance(position, existingPos) < minDistance)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                    
                    if (!tooClose)
                    {
                        positions.Add(position);
                    }
                }
                
                currentAttempts++;
            }

            return positions;
        }

        private void GenerateObjects()
        {
            if (prefab == null) return;

            GameObject container = new GameObject("Generated_Objects");
            Undo.RegisterCreatedObjectUndo(container, "Generate Objects");

            List<Vector3> positions = GeneratePositions();

            foreach (Vector3 basePosition in positions)
            {
                Vector3 finalPosition = basePosition;

                // Raycast to find surface
                if (alignToSurface)
                {
                    RaycastHit hit;
                    Vector3 rayStart = finalPosition + Vector3.up * 1000f;
                    if (Physics.Raycast(rayStart, Vector3.down, out hit, Mathf.Infinity, surfaceLayer))
                    {
                        finalPosition = hit.point;
                        Debug.DrawLine(rayStart, hit.point, Color.green, 2f);
                    }
                    else
                    {
                        finalPosition.y = 0f;
                        Debug.DrawLine(rayStart, finalPosition, Color.red, 2f);
                        Debug.LogWarning($"Raycast failed at position {finalPosition}. Setting y to 0.");
                    }
                }
                else
                {
                    finalPosition.y = 0f;
                }

                // オフセットを適用
                finalPosition.y += yOffset;

                // Create object
                GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                obj.transform.position = finalPosition;
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