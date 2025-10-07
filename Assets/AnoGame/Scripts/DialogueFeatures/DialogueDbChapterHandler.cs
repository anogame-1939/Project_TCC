using System;
using System.Collections.Generic;
using UnityEngine;
using PixelCrushers.DialogueSystem;
using UnityEngine.Events;

namespace AnoGame.Apllication.DialogueFeatures
{
    // DialogueDbChapterHandler.cs
    // 使い方：シーンのどこかに置く（DialogueManagerと同じシーン）
    //  - Base Databases には常時使うDB（共通/マスター）を設定
    //  - Chapters[n] には各章（ワールド）で使うDB群を設定
    //  - Start時に initialChapterIndex が0以上ならその章をロード
    //  - ゲーム中は LoadChapter(int) / LoadChapterById(string) を呼ぶだけ

    [DisallowMultipleComponent]
    public sealed class DialogueDbChapterHandler : MonoBehaviour
    {
        [Header("Always-on (Base)")]
        [Tooltip("常時ロードしておく共通DB（タイトル重複NG）")]
        [SerializeField] private DialogueDatabase[] baseDatabases;

        [Serializable]
        public class ChapterEntry
        {
            public string chapterId; // 任意のID（例: CH1 / Prologue / World_A など）
            public DialogueDatabase[] databases;
        }

        [Header("Per Chapter/World")]
        [SerializeField] private ChapterEntry[] chapters;

        [Header("Boot")]
        [Tooltip("起動時に自動でロードする章。-1なら何もしない")]
        [SerializeField] private int initialChapterIndex = -1;

        [Header("Options")]
        [Tooltip("章切替後、セーブ済み状態を再適用する（Instantiate Database使用時など）")]
        [SerializeField] private bool applyPersistentDataAfterSwitch = false;

        [Header("Events")]
        public UnityEvent<int> onChapterLoaded;     // 引数: index
        public UnityEvent<string> onChapterLoadedId; // 引数: chapterId

        private int _currentChapterIndex = -1;

        // 自分が「追加したDB」だけ記録しておき、Remove時に誤って初期DBを消さないようにする
        private readonly HashSet<DialogueDatabase> _addedDatabases = new HashSet<DialogueDatabase>();

        private void Start()
        {
            // ベースDBを加算ロード（すでにInitial Databaseに入っているものがあっても、Remove側で触らない設計）
            foreach (var db in baseDatabases)
            {
                SafeAdd(db);
            }

            if (initialChapterIndex >= 0 && initialChapterIndex < chapters.Length)
            {
                LoadChapter(initialChapterIndex);
            }
        }

        /// <summary>
        /// 章をインデックスで切り替える（前章DBを外してから新章DBを加算）
        /// </summary>
        public void LoadChapter(int chapterIndex)
        {
            if (chapterIndex < 0 || chapterIndex >= chapters.Length) return;
            if (_currentChapterIndex == chapterIndex) return;

            // 旧章DBをアンロード（自分がAddしたものだけ）
            if (IsValidIndex(_currentChapterIndex))
            {
                foreach (var db in chapters[_currentChapterIndex].databases)
                {
                    SafeRemove(db);
                }
            }

            _currentChapterIndex = chapterIndex;

            // 新章DBをロード
            foreach (var db in chapters[_currentChapterIndex].databases)
            {
                SafeAdd(db);
            }

            if (applyPersistentDataAfterSwitch)
            {
                // 既にセーブ済みの変数/クエスト状態を再反映したい場合のみ
                PersistentDataManager.Apply();
            }

            onChapterLoaded?.Invoke(_currentChapterIndex);
            onChapterLoadedId?.Invoke(chapters[_currentChapterIndex].chapterId);
        }

        /// <summary>
        /// 章をIDで切り替える
        /// </summary>
        public void LoadChapterById(string chapterId)
        {
            var idx = Array.FindIndex(chapters, c => c != null && c.chapterId == chapterId);
            if (idx >= 0) LoadChapter(idx);
        }

        /// <summary>
        /// 現在の章DBだけアンロード（ベースは残す）
        /// </summary>
        public void UnloadCurrentChapter()
        {
            if (!IsValidIndex(_currentChapterIndex)) return;
            foreach (var db in chapters[_currentChapterIndex].databases)
            {
                SafeRemove(db);
            }
            _currentChapterIndex = -1;
        }

        private bool IsValidIndex(int idx) => idx >= 0 && idx < chapters.Length;

        private void SafeAdd(DialogueDatabase db)
        {
            if (db == null) return;
            // 既に自分が追加済みならスキップ（Initialとして最初から入っている分は触らない）
            if (_addedDatabases.Contains(db))
            {
                return;
            }

            DialogueManager.AddDatabase(db);
            _addedDatabases.Add(db);
        }

        private void SafeRemove(DialogueDatabase db)
        {
            if (db == null) return;
            // 自分がAddしたものだけRemoveする（Initialに設定されているDBを誤って外さないため）
            if (_addedDatabases.Remove(db))
            {
                DialogueManager.RemoveDatabase(db);
            }
        }
    }

}
