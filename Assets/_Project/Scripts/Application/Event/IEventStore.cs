using System.Collections.Generic;

namespace AnoGame.AnoFlow
{
    /// <summary>
    /// イベント履歴の永続化を担うインターフェース。
    /// ゲーム側で実装し、DIまたはServiceLocator経由で注入する。
    /// </summary>
    public interface IEventStore
    {
        void AddEvent(string eventId);
        void AddTags(IEnumerable<string> tags);
    }
}
