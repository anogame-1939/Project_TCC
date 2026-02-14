using System;
using System.Collections.Generic;

namespace AnoGame.Domain.Event.Services
{
    public interface IEventService
    {
        event Action LoadedClearEvent;
        void SetClearedEvents(HashSet<string> clearedEventIDs);
        bool IsEventCleared(string eventID);
        void RemoveClearedEvent(string eventID);
        void RegisterStartEventHandler(string eventID, System.Action handler);
        void RegisterCompleteEventHandler(string eventID, System.Action handler);
        void RegisterFailedEventHandler(string eventID, System.Action handler);
        void UnregisterStartEventHandler(string eventID, System.Action handler);
        void UnregisterCompleteEventHandler(string eventID, System.Action handler);
        void UnregisterFailedEventHandler(string eventID, System.Action handler);
        void TriggerEventStart(string eventID);
        void TriggerEventComplete(string eventID);
        void TriggerEventFailed(string eventID);

        // --- Tag System ---
        /// <summary>指定タグがアクティブかどうかを判定</summary>
        bool HasTag(string tag);
        /// <summary>タグを追加する（resultTags付与用）</summary>
        void AddTags(IEnumerable<string> tags);
        /// <summary>現在のアクティブタグ一覧を取得（セーブ用）</summary>
        HashSet<string> GetActiveTags();
        /// <summary>アクティブタグを一括設定（ロード用）</summary>
        void SetActiveTags(HashSet<string> tags);
    }
}
