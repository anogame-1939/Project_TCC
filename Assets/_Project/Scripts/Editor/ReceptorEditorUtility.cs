#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using AnoGame.AnoFlow.Editor;

namespace AnoGame.Editor
{
    /// <summary>
    /// Receptor 系エディタ共通の重複イベントバインディングチェックユーティリティ。
    /// 同じ EventData が既に別の AnoEventRoot で使用されている場合に警告ダイアログを表示する。
    /// </summary>
    public static class ReceptorEditorUtility
    {
        /// <summary>
        /// targetEventId の PropertyField に重複チェックを設定する。
        /// 値が実際に変化した時のみ（前回値と比較）、かつ遅延実行で1回だけ警告を出す。
        /// </summary>
        public static void SetupDuplicateCheck(
            UnityEditor.UIElements.PropertyField eventField,
            SerializedObject serializedObject,
            string eventIdPropertyName,
            Component currentComponent)
        {
            // 現在の値を記憶
            serializedObject.Update();
            var initialProp = serializedObject.FindProperty(eventIdPropertyName);
            string lastKnownValue = initialProp != null ? initialProp.stringValue : "";

            eventField.RegisterValueChangeCallback(evt =>
            {
                serializedObject.Update();
                var prop = serializedObject.FindProperty(eventIdPropertyName);
                if (prop == null) return;

                string currentValue = prop.stringValue;

                // 値が実際に変化していなければスキップ
                if (currentValue == lastKnownValue) return;
                lastKnownValue = currentValue;

                if (string.IsNullOrEmpty(currentValue)) return;

                var containers = EventSceneBinder.FindAllContainers(currentValue);
                if (containers.Count == 0) return;

                // 自分自身の AnoEventRoot を除外
                var myRoot = currentComponent.GetComponentInParent<Application.Event.AnoEventRoot>(true);

                int otherCount = 0;
                string otherNames = "";

                foreach (var container in containers)
                {
                    var root = container.GetComponent<Application.Event.AnoEventRoot>();
                    if (root == null || root == myRoot) continue;

                    otherCount++;
                    if (otherNames.Length > 0) otherNames += ", ";
                    otherNames += container.name;
                }

                if (otherCount > 0)
                {
                    // 遅延実行でダイアログ表示（UIイベント処理の外で実行）
                    EditorApplication.delayCall += () =>
                    {
                        EditorUtility.DisplayDialog(
                            "イベント重複警告",
                            $"このイベント ({currentValue}) は既に以下のオブジェクトで使用されています:\n\n{otherNames}\n\n同じイベントを複数のオブジェクトに設定しても良いか確認してください。",
                            "OK");
                    };
                }
            });
        }
    }
}
#endif
