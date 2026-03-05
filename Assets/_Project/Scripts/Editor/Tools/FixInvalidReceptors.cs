using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AnoGame.AnoFlow;

namespace AnoGame.Editor.Tools
{
    /// <summary>
    /// 不正な targetEventId を持つ InspectReceptor / ContactReceptor を検出・修正するツール
    /// </summary>
    public static class FixInvalidReceptors
    {
        [MenuItem("AnoGame/Tools/不正なReceptorを検出 (ドライラン)")]
        public static void DetectInvalidReceptors()
        {
            ScanReceptors(dryRun: true);
        }

        [MenuItem("AnoGame/Tools/不正なReceptorを無効化して修正")]
        public static void FixInvalidReceptorsExecute()
        {
            if (!EditorUtility.DisplayDialog(
                "不正なReceptorを修正",
                "targetEventIdが数字のみのReceptorオブジェクトを無効化します。\n実行しますか？",
                "実行", "キャンセル"))
            {
                return;
            }

            ScanReceptors(dryRun: false);
        }

        private static void ScanReceptors(bool dryRun)
        {
            // 数字のみのパターン（インデックス値が入っている）
            var numberOnlyRegex = new Regex(@"^\d+$");

            var inspectReceptors = GameObject.FindObjectsOfType<InspectReceptor>(true);
            var invalidList = new List<(GameObject go, string targetId)>();

            foreach (var receptor in inspectReceptors)
            {
                var so = new SerializedObject(receptor);
                var targetIdProp = so.FindProperty("targetEventId");
                if (targetIdProp == null) continue;

                string targetId = targetIdProp.stringValue;

                // targetEventId が数字のみ → 不正値（ドロップダウンのインデックス）
                if (!string.IsNullOrEmpty(targetId) && numberOnlyRegex.IsMatch(targetId))
                {
                    invalidList.Add((receptor.gameObject, targetId));
                }
            }

            // ContactReceptor も同様にチェック
            var contactReceptors = GameObject.FindObjectsOfType<ContactReceptor>(true);
            foreach (var receptor in contactReceptors)
            {
                var so = new SerializedObject(receptor);
                var targetIdProp = so.FindProperty("targetEventId");
                if (targetIdProp == null) continue;

                string targetId = targetIdProp.stringValue;

                if (!string.IsNullOrEmpty(targetId) && numberOnlyRegex.IsMatch(targetId))
                {
                    invalidList.Add((receptor.gameObject, targetId));
                }
            }

            if (invalidList.Count == 0)
            {
                Debug.Log("[FixReceptors] 不正なReceptorは見つかりませんでした。");
                return;
            }

            Debug.Log($"[FixReceptors] 不正なReceptor: {invalidList.Count}件検出");
            foreach (var (go, targetId) in invalidList)
            {
                Debug.Log($"  ❌ {go.name} | targetEventId=\"{targetId}\" | pos={go.transform.position}");

                if (!dryRun)
                {
                    Undo.RecordObject(go, "Disable Invalid Receptor");
                    go.SetActive(false);
                    EditorUtility.SetDirty(go);
                    Debug.Log($"  → 無効化しました: {go.name}");
                }
            }

            if (!dryRun)
            {
                Debug.Log($"[FixReceptors] {invalidList.Count}件のReceptorを無効化しました。シーンを保存してください。");
            }
            else
            {
                Debug.Log($"[FixReceptors] ドライラン完了。「不正なReceptorを無効化して修正」で実行してください。");
            }
        }
    }
}
