using System.Collections.Generic;
using UnityEngine;
using PixelCrushers.DialogueSystem;

#if UNITY_EDITOR
// Editor だけ詳細ログにしたい場合はここを使うか、インスペクタのトグルで制御してください。
#endif

namespace PixelCrushers.DialogueSystem
{
    /// <summary>
    /// 会話のカスタム Bool フィールド:
    /// - "Overlay"   : true なら頭上吹き出し → Standard のデフォルトパネルに切替
    /// - "Flashback" : true なら回想モード（ここではログのみ / 必要なら処理を追加）
    /// </summary>
    [DisallowMultipleComponent]
    public class MultipleDialogueUI : StandardDialogueUI
    {
        [Header("Debug")]
        [Tooltip("詳細ログを有効にします。")]
        [SerializeField] private bool verboseLogging = true;

        private readonly Dictionary<DialogueActor, SubtitlePanelNumber> _savedPanels =
            new Dictionary<DialogueActor, SubtitlePanelNumber>();

        private string Prefix => "[MultipleDialogueUI] ";

        public override void Open()
        {
            _savedPanels.Clear();

            var lastConvTitle = DialogueManager.lastConversationStarted;
            var db = DialogueManager.MasterDatabase;
            var conv = (db != null && !string.IsNullOrEmpty(lastConvTitle))
                ? db.GetConversation(lastConvTitle)
                : null;

            Log($"Open()  start. lastConversationStarted='{lastConvTitle}', DB={(db ? "OK" : "NULL")}");

            if (conv == null)
            {
                LogWarn($"会話が取得できませんでした。Overlay/Flashback 処理はスキップします。");
                base.Open();
                return;
            }

            // 会話レベルのフラグを読む
            bool overlay   = conv.LookupBool("Overlay");
            bool flashback = conv.LookupBool("Flashback");
            Log($"Conversation '{conv.Title}' flags: Overlay={overlay}, Flashback={flashback}");

            if (overlay)
            {
                // 会話の主役/相手（開始時点）
                TrySetActorToDefault(DialogueActor.GetDialogueActorComponent(DialogueManager.currentActor), "currentActor");
                TrySetActorToDefault(DialogueActor.GetDialogueActorComponent(DialogueManager.currentConversant), "currentConversant");

                // 会話中の全エントリに現れる Actor も対象にする
                var entries = conv.dialogueEntries;
                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    TrySetActorToDefault(entry.ActorID, $"dialogueEntries[{i}].ActorID");
                    TrySetConversantToDefault(entry.ConversantID, $"dialogueEntries[{i}].ConversantID");
                }
            }

            if (flashback)
            {
                // ここで BGM/SE, 画面効果, 色味トーン, 入力ロック等の回想演出をフックできます
                Log("Flashback フラグを検出。必要な回想演出があればここで開始してください。");
            }

            base.Open();
            Log("Open()  end.");
        }

        public override void Close()
        {
            base.Close();
            Log($"Close()  start. restore count={_savedPanels.Count}");

            // 変更した Actor の subtitlePanelNumber を元に戻す
            foreach (var kvp in _savedPanels)
            {
                var actor = kvp.Key;
                if (actor == null)
                {
                    LogWarn("復元対象の DialogueActor が破棄されています。スキップ。");
                    continue;
                }
                actor.standardDialogueUISettings.subtitlePanelNumber = kvp.Value;
                Log($"Restored panel: Actor='{actor.name}' -> {kvp.Value}");
            }

            _savedPanels.Clear();
            Log("Close()  end.");
        }

        // ---------- 内部: Actor 取得と切替補助 ----------

        private void TrySetActorToDefault(DialogueActor actor, string srcTag)
        {
            if (actor == null)
            {
                LogWarn($"SetDefault SKIP ({srcTag}): DialogueActor=NULL");
                return;
            }
            if (_savedPanels.ContainsKey(actor))
            {
                Log($"SetDefault SKIP ({srcTag}): Actor='{actor.name}' は既に処理済み");
                return;
            }

            var current = actor.standardDialogueUISettings.subtitlePanelNumber;
            _savedPanels[actor] = current; // 退避
            actor.standardDialogueUISettings.subtitlePanelNumber = SubtitlePanelNumber.Default;

            Log($"SetDefault OK   ({srcTag}): Actor='{actor.name}', {current} -> Default");
        }

        private void TrySetActorToDefault(int actorID, string srcTag)
        {
            if (actorID <= 0)
            {
                LogWarn($"SetDefault SKIP ({srcTag}): actorID={actorID}");
                return;
            }

            var db = DialogueManager.MasterDatabase;
            if (db == null)
            {
                LogError($"SetDefault FAIL ({srcTag}): MasterDatabase=NULL");
                return;
            }

            var actorAsset = db.GetActor(actorID);
            if (actorAsset == null)
            {
                LogWarn($"SetDefault SKIP ({srcTag}): ActorAsset が見つかりません (ID={actorID})");
                return;
            }

            // ランタイムの DialogueActor コンポーネントを逆引き
            var tr = CharacterInfo.GetRegisteredActorTransform(actorAsset.Name);
            if (tr == null)
            {
                LogWarn($"SetDefault SKIP ({srcTag}): '{actorAsset.Name}' の Transform 未登録");
                return;
            }

            var dialogueActor = DialogueActor.GetDialogueActorComponent(tr);
            if (dialogueActor == null)
            {
                LogWarn($"SetDefault SKIP ({srcTag}): '{actorAsset.Name}' に DialogueActor コンポーネント無し");
                return;
            }

            TrySetActorToDefault(dialogueActor, $"{srcTag} ('{actorAsset.Name}')");
        }

        private void TrySetConversantToDefault(int conversantID, string srcTag)
        {
            // conversant 側も同様に追跡しておくと、頭上吹き出しから漏れにくいです
            TrySetActorToDefault(conversantID, srcTag.Replace("ActorID", "ConversantID"));
        }

        // ---------- ログ補助 ----------

        private void Log(string msg)
        {
            if (verboseLogging) Debug.Log($"{Prefix}{msg}", this);
        }

        private void LogWarn(string msg)
        {
            if (verboseLogging) Debug.LogWarning($"{Prefix}{msg}", this);
        }

        private void LogError(string msg)
        {
            Debug.LogError($"{Prefix}{msg}", this);
        }
    }
}
