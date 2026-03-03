using UnityEngine;
using UnityEditor;

namespace AnoGame.Editor.Tools
{
    /// <summary>
    /// grpGateプレハブにピボットラッパーを追加し、
    /// ElectronicDoorControllerの参照を自動配線するワンタイムセットアップツール
    /// セットアップ完了後は削除して構いません
    /// </summary>
    public static class GatePivotSetupTool
    {
        private const string PrefabPath = "Assets/Anime Tokyo/Yuu/grpGate.prefab";
        private const string GateLeftName = "SM_fence_gate_suburbs_01_2";
        private const string GateRightName = "SM_fence_gate_suburbs_01_3";
        private const string PanelName = "\u958B\u9589\u30B3\u30F3\u30C8\u30ED\u30FC\u30EB\u30D1\u30CD\u30EB";

        [MenuItem("Tools/AnoGame/Setup Gate Pivots (grpGate)")]
        public static void SetupGatePivots()
        {
            // プレハブを開く
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefabAsset == null)
            {
                Debug.LogError($"[GatePivotSetup] Prefab not found: {PrefabPath}");
                return;
            }

            // プレハブ編集コンテキストを開く
            var prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);

            try
            {
                var gateLeft = FindChild(prefabRoot.transform, GateLeftName);
                var gateRight = FindChild(prefabRoot.transform, GateRightName);
                var panel = FindChild(prefabRoot.transform, PanelName);

                if (gateLeft == null)
                {
                    Debug.LogError($"[GatePivotSetup] '{GateLeftName}' not found in prefab.");
                    return;
                }
                if (gateRight == null)
                {
                    Debug.LogError($"[GatePivotSetup] '{GateRightName}' not found in prefab.");
                    return;
                }

                // 既にピボットラッパーがある場合はスキップ
                if (prefabRoot.transform.Find("GatePivot_Left") != null)
                {
                    Debug.LogWarning("[GatePivotSetup] Pivot wrappers already exist. Skipping.");
                    return;
                }

                // --- GatePivot_Left ---
                var pivotLeftGO = new GameObject("GatePivot_Left");
                pivotLeftGO.transform.SetParent(prefabRoot.transform, false);
                // ピボット位置 = 門の現在位置（メッシュ原点がヒンジ側の端にある）
                pivotLeftGO.transform.localPosition = gateLeft.localPosition;
                pivotLeftGO.transform.localRotation = gateLeft.localRotation;
                // 門をピボットの子にする（ローカル位置をゼロに）
                gateLeft.SetParent(pivotLeftGO.transform, true);
                gateLeft.localPosition = Vector3.zero;
                gateLeft.localRotation = Quaternion.identity;

                // --- GatePivot_Right ---
                var pivotRightGO = new GameObject("GatePivot_Right");
                pivotRightGO.transform.SetParent(prefabRoot.transform, false);
                pivotRightGO.transform.localPosition = gateRight.localPosition;
                pivotRightGO.transform.localRotation = gateRight.localRotation;
                gateRight.SetParent(pivotRightGO.transform, true);
                gateRight.localPosition = Vector3.zero;
                gateRight.localRotation = Quaternion.identity;

                // --- ElectronicDoorController の参照配線 ---
                var controller = prefabRoot.GetComponent<Application.Gimmicks.ElectronicDoorController>();
                if (controller == null)
                {
                    controller = prefabRoot.AddComponent<Application.Gimmicks.ElectronicDoorController>();
                    Debug.Log("[GatePivotSetup] Added ElectronicDoorController component.");
                }

                // SerializedObjectで参照を設定
                var so = new SerializedObject(controller);
                so.FindProperty("_gatePivotLeft").objectReferenceValue = pivotLeftGO.transform;
                so.FindProperty("_gatePivotRight").objectReferenceValue = pivotRightGO.transform;

                if (panel != null)
                {
                    var panelRenderer = panel.GetComponent<Renderer>();
                    if (panelRenderer != null)
                    {
                        so.FindProperty("_panelRenderer").objectReferenceValue = panelRenderer;
                        Debug.Log("[GatePivotSetup] Panel renderer assigned.");
                    }
                }

                so.ApplyModifiedProperties();

                // プレハブを保存
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
                Debug.Log("[GatePivotSetup] Gate pivots setup completed successfully!");
                Debug.Log("[GatePivotSetup] Structure:");
                Debug.Log("  grpGate");
                Debug.Log("    +-- GatePivot_Left");
                Debug.Log($"    |     +-- {GateLeftName}");
                Debug.Log("    +-- GatePivot_Right");
                Debug.Log($"    |     +-- {GateRightName}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static Transform FindChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;
            }
            return null;
        }
    }
}
