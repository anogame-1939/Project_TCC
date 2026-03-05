#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using AnoGame.Application.Event;
using AnoGame.AnoFlow;
using System.Collections.Generic;

namespace AnoGame.Editor.Tools
{
    /// <summary>
    /// 既存のシーン内のイベントオブジェクトに AnoEventRoot をアタッチするマイグレーションツール
    /// </summary>
    public class EventDashboardMigration
    {
        [MenuItem("AnoGame/Tools/Event Dashboard/Migrate Scene Events to AnoEventRoot")]
        public static void MigrateCurrentScene()
        {
            var triggers = Object.FindObjectsByType<InstantEventTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int migratedCount = 0;
            int alreadyExistsCount = 0;

            foreach (var trigger in triggers)
            {
                // Triggerオブジェクトの親がルートオブジェクトであると仮定
                Transform rootTransform = trigger.transform.parent;
                if (rootTransform == null) continue;

                GameObject rootObj = rootTransform.gameObject;
                
                AnoEventRoot eventRoot = rootObj.GetComponent<AnoEventRoot>();
                if (eventRoot != null)
                {
                    alreadyExistsCount++;
                    continue; // 既にアタッチ済み
                }

                // AnoEventRoot を追加
                eventRoot = Undo.AddComponent<AnoEventRoot>(rootObj);

                // 子から Receptor を探す
                GameObject receptorObj = null;
                foreach (Transform child in rootTransform)
                {
                    if (child.name.Contains("Receptor") || child.GetComponent<ContactReceptor>() != null || child.GetComponent<InspectReceptor>() != null || child.GetComponent<ItemReceptor>() != null)
                    {
                        receptorObj = child.gameObject;
                        break;
                    }
                }

                AnoGame.Data.EventData foundEventData = null;

                // Receptor から EventData を抽出してみる
                if (receptorObj != null)
                {
                    var contact = receptorObj.GetComponent<ContactReceptor>();
                    if (contact != null) foundEventData = GetEventData(contact);

                    if (foundEventData == null)
                    {
                        var inspect = receptorObj.GetComponent<InspectReceptor>();
                        if (inspect != null) foundEventData = GetEventData(inspect);
                    }

                    if (foundEventData == null)
                    {
                        var item = receptorObj.GetComponent<ItemReceptor>();
                        if (item != null) foundEventData = GetEventData(item);
                    }
                }

                // イベントデータが取得できなかった場合は、TriggerからターゲットIDを取得してAssetDatabaseから検索
                if (foundEventData == null)
                {
                    SerializedObject so = new SerializedObject(trigger);
                    var pTargetEventId = so.FindProperty("targetEventId");
                    if (pTargetEventId != null && !string.IsNullOrEmpty(pTargetEventId.stringValue))
                    {
                        string eventId = pTargetEventId.stringValue;
                        string[] guids = AssetDatabase.FindAssets($"t:EventData {eventId}");
                        foreach (var guid in guids)
                        {
                            string path = AssetDatabase.GUIDToAssetPath(guid);
                            var asset = AssetDatabase.LoadAssetAtPath<AnoGame.Data.EventData>(path);
                            if (asset != null && asset.EventId == eventId)
                            {
                                foundEventData = asset;
                                break;
                            }
                        }
                    }
                }

                Undo.RecordObject(eventRoot, "Setup AnoEventRoot");
                eventRoot.Setup(foundEventData, receptorObj, trigger.gameObject);
                
                migratedCount++;
            }

            if (migratedCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                Debug.Log($"[Event Dashboard] マイグレーション完了: {migratedCount} 件のイベントオブジェクトに AnoEventRoot を追加・設定しました。（既に設定済み: {alreadyExistsCount}件）");
            }
            else
            {
                Debug.Log($"[Event Dashboard] マイグレーション対象が見つかりませんでした。（既に設定済み: {alreadyExistsCount}件）");
            }
        }

        private static AnoGame.Data.EventData GetEventData(Object receptor)
        {
            SerializedObject so = new SerializedObject(receptor);
            var pEventData = so.FindProperty("eventData");
            if (pEventData != null && pEventData.objectReferenceValue != null)
            {
                return pEventData.objectReferenceValue as AnoGame.Data.EventData;
            }
            return null;
        }
    }
}
#endif
