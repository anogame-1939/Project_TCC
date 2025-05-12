using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AnoGame.EditorExtensions
{
    public class ProceduralLinePlacementTool : EditorWindow
    {
        private GameObject prefab;
        private int count = 10;
        private float spacing = 2f;
        private float jitterAmount = 0.5f;
        private float yOffset = 0f;
        private bool randomRotation = true;
        private float minScale = 1f;
        private float maxScale = 1f;
        private GameObject containerObject;

        [MenuItem("Tools/Procedural Line Placement")]
        public static void ShowWindow()
        {
            GetWindow<ProceduralLinePlacementTool>("Line Placement");
        }

        private void OnGUI()
        {
            GUILayout.Label("Line Placement Settings", EditorStyles.boldLabel);

            prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false);
            count = EditorGUILayout.IntField("Count", count);
            spacing = EditorGUILayout.FloatField("Spacing", spacing);
            jitterAmount = EditorGUILayout.FloatField("Jitter Amount", jitterAmount);
            yOffset = EditorGUILayout.FloatField("Y Offset", yOffset);

            EditorGUILayout.Space();
            GUILayout.Label("Transform Settings", EditorStyles.boldLabel);
            randomRotation = EditorGUILayout.Toggle("Random Y Rotation", randomRotation);
            minScale = EditorGUILayout.FloatField("Min Scale", minScale);
            maxScale = EditorGUILayout.FloatField("Max Scale", maxScale);

            EditorGUILayout.Space();
            GUI.enabled = prefab != null && count > 0;
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
            var positions = new List<Vector3>(count);
            for (int i = 0; i < count; i++)
            {
                // ベースの位置（X 軸方向に spacing ごとに並べる）
                Vector3 basePos = new Vector3(i * spacing, 0f, 0f);

                // XZ 平面でランダムにばらつきを追加
                Vector2 jitter = Random.insideUnitCircle * jitterAmount;
                Vector3 finalPos = new Vector3(
                    basePos.x + jitter.x,
                    yOffset,
                    basePos.z + jitter.y
                );

                positions.Add(finalPos);
            }
            return positions;
        }

        private void GenerateObjects()
        {
            if (prefab == null || count <= 0) return;

            // 既存のコンテナがあれば削除
            ClearGenerated();

            // 新しいコンテナを作成
            containerObject = new GameObject("LineGenerated_Objects");
            Undo.RegisterCreatedObjectUndo(containerObject, "Create Line Container");

            var positions = GeneratePositions();
            foreach (var pos in positions)
            {
                var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Undo.RegisterCreatedObjectUndo(obj, "Place Object");

                obj.transform.position = pos;
                obj.transform.SetParent(containerObject.transform);

                // ランダム回転
                if (randomRotation)
                {
                    obj.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                }

                // ランダムスケール
                float scale = Random.Range(minScale, maxScale);
                obj.transform.localScale = Vector3.one * scale;
            }
        }

        private void ClearGenerated()
        {
            if (containerObject != null)
            {
                Undo.DestroyObjectImmediate(containerObject);
                containerObject = null;
            }
            else
            {
                // シーン内に前回生成したコンテナがあればまとめて削除
                var existing = GameObject.Find("LineGenerated_Objects");
                if (existing != null)
                {
                    Undo.DestroyObjectImmediate(existing);
                }
            }
        }
    }
}
