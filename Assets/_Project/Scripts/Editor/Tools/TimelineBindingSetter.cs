using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEditor;

namespace AnoGame.Editor.Tools
{
    /// <summary>
    /// GeneratedEvents配下のPlayableDirectorに
    /// DialogueTimelineReceiver / InventoryHandler / 共通SignalReceiver のバインディングを設定するツール
    /// </summary>
    public static class TimelineBindingSetter
    {
        [MenuItem("AnoGame/Tools/Set Timeline Track Bindings")]
        public static void SetAllBindings()
        {
            // LevelModules配下のバインディングターゲットを検索
            var dialogueReceiver = FindComponentByName<MonoBehaviour>("DialogueTimelineReceiver");
            var inventoryHandler = FindComponentByName<MonoBehaviour>("InventoryHandler");
            var signalReceiver = FindComponentByName<SignalReceiver>("共通のSignalReceiver");

            if (dialogueReceiver == null)
            {
                Debug.LogError("[TimelineBindingSetter] DialogueTimelineReceiver が見つかりません。LevelModulesがシーンに配置されているか確認してください。");
                return;
            }
            if (inventoryHandler == null)
            {
                Debug.LogError("[TimelineBindingSetter] InventoryHandler が見つかりません。");
                return;
            }

            // GeneratedEvents配下の全PlayableDirectorを取得
            var generatedEvents = GameObject.Find("GeneratedEvents");
            if (generatedEvents == null)
            {
                Debug.LogError("[TimelineBindingSetter] GeneratedEvents が見つかりません。");
                return;
            }

            var directors = generatedEvents.GetComponentsInChildren<PlayableDirector>(true);
            int boundCount = 0;

            foreach (var director in directors)
            {
                if (director.playableAsset is not TimelineAsset timeline) continue;

                bool modified = false;
                foreach (var track in timeline.GetOutputTracks())
                {
                    var existing = director.GetGenericBinding(track);

                    if (track.name == "Ano Narrative Track" && existing == null)
                    {
                        director.SetGenericBinding(track, dialogueReceiver);
                        modified = true;
                        Debug.Log($"[TimelineBindingSetter] {director.gameObject.name}: Ano Narrative Track → DialogueTimelineReceiver");
                    }
                    else if (track.name == "Inventory Item Track" && existing == null)
                    {
                        director.SetGenericBinding(track, inventoryHandler);
                        modified = true;
                        Debug.Log($"[TimelineBindingSetter] {director.gameObject.name}: Inventory Item Track → InventoryHandler");
                    }
                    else if (track is SignalTrack && existing == null && signalReceiver != null)
                    {
                        director.SetGenericBinding(track, signalReceiver);
                        modified = true;
                        Debug.Log($"[TimelineBindingSetter] {director.gameObject.name}: Signal Track → 共通SignalReceiver");
                    }
                }

                if (modified)
                {
                    EditorUtility.SetDirty(director);
                    boundCount++;
                }
            }

            Debug.Log($"[TimelineBindingSetter] 完了: {boundCount} 件のPlayableDirectorを更新しました。");
        }

        private static T FindComponentByName<T>(string gameObjectName) where T : Component
        {
            var allObjects = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                if (obj.gameObject.name == gameObjectName)
                    return obj;
            }
            return null;
        }
    }
}
