using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace AnoGame.AnoNarrative.Editor
{
    public class DialogueGraphSidebar
    {
        public MasterDialogueData Data;
        private Vector2 _scrollPos;
        private string _searchFilter = "";

        // Navigation events
        public System.Action<Vector2> OnRequestPanTo;
        public System.Action<int, int, int, string> OnSelectSection;

        // Static変数でドラッグデータを保持
        private static SectionDragData _currentDragData;

        public DialogueGraphSidebar(MasterDialogueData data)
        {
            Data = data;
        }

        public void Draw(float width)
        {
            // イベント処理：ドラッグ終了や中断時の強制リセット
            Event evt = Event.current;
            if (evt.type == EventType.DragExited || evt.type == EventType.Ignore || (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Escape))
            {
                if (_currentDragData != null)
                {
                    _currentDragData = null;
                    // DragAndDrop.PrepareStartDrag(); 
                }
            }
            // マウスアップ時も、ドラッグ中ならリセット（ドロップ処理漏れ防止）
            if (evt.type == EventType.MouseUp && _currentDragData != null)
            {
                // ここでnullにするとDragPerformが呼ばれる前に消えてしまう可能性があるため、
                // 本来はDragPerformで消すが、保険としてGUIの最後に消す処理を入れるのが一般的。
                // 今回はDragPerformが呼ばれない問題への対処なので、ここは一旦スルーしてOK。
            }

            GUILayout.BeginVertical(GUILayout.Width(width), GUILayout.ExpandHeight(true));
            EditorGUILayout.LabelField("Navigator", EditorStyles.boldLabel);

            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            if (Data != null)
            {
                var episodes = Data.Conversations.GroupBy(u => u.EpisodeID).OrderBy(g => g.Key);

                foreach (var epGroup in episodes)
                {
                    string epStr = epGroup.Key == -1 ? "Default" : epGroup.Key.ToString();
                    bool allowEp = string.IsNullOrEmpty(_searchFilter) || epStr.Contains(_searchFilter);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Episode: {epStr}", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(20)))
                    {
                        CreateChapter(epGroup.Key);
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUI.indentLevel++;

                    var chapters = epGroup.GroupBy(u => u.ChapterID).OrderBy(g => g.Key);

                    foreach (var chapterGroup in chapters)
                    {
                        string chStr = chapterGroup.Key == -1 ? "Default" : chapterGroup.Key.ToString();
                        bool allowChapter = allowEp || chStr.Contains(_searchFilter);

                        // =========================================================
                        // 【修正点1】GetLastRectをやめ、GetControlRectで確実に領域を確保
                        // =========================================================

                        // 1行分のRectを確保する（ここをチャプターヘッダーとドロップエリアにする）
                        Rect headerRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

                        // 判定用Rect（横幅いっぱい）
                        Rect dropRect = new Rect(0, headerRect.y, width, headerRect.height);

                        // ラベルなどの描画用Rect調整
                        Rect labelRect = new Rect(headerRect.x, headerRect.y, headerRect.width - 50, headerRect.height);

                        // 1. ラベル描画（手動）
                        EditorGUI.LabelField(labelRect, $"{chStr}", EditorStyles.miniBoldLabel);

                        // 2. ドロップ判定処理
                        bool blockedByButton = (evt.mousePosition.x > width - 50);

                        if (dropRect.Contains(evt.mousePosition))
                        {
                            if (_currentDragData != null)
                            {
                                if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
                                {
                                    DragAndDrop.visualMode = blockedByButton ? DragAndDropVisualMode.None : DragAndDropVisualMode.Move;

                                    if (evt.type == EventType.DragUpdated)
                                    {
                                        if (!blockedByButton)
                                        {
                                            EditorGUI.DrawRect(dropRect, new Color(1f, 1f, 0f, 0.3f));
                                            Event.current.Use();
                                        }
                                    }

                                    if (evt.type == EventType.DragPerform)
                                    {
                                        if (!blockedByButton)
                                        {
                                            Debug.Log($"Dropped on Chapter {chapterGroup.Key}.");
                                            DragAndDrop.AcceptDrag();

                                            MoveSectionToChapter(_currentDragData.Ep, _currentDragData.Ch, _currentDragData.Sec, epGroup.Key, chapterGroup.Key);

                                            _currentDragData = null;
                                            DragAndDrop.PrepareStartDrag();
                                            Event.current.Use();
                                        }
                                    }
                                }
                            }
                        }

                        // 3. ボタン描画（手動配置）
                        // GetControlRectを使ったので、GUILayout.ButtonではなくGUI.Buttonを使う
                        // またはGUILayoutのエリア計算が狂わないよう注意が必要ですが、
                        // ここではシンプルにRect計算でボタンを配置します。

                        Rect btnRectPlus = new Rect(headerRect.xMax - 42, headerRect.y, 20, headerRect.height);
                        Rect btnRectMinus = new Rect(headerRect.xMax - 20, headerRect.y, 20, headerRect.height);

                        if (GUI.Button(btnRectPlus, "+", EditorStyles.miniButtonLeft))
                        {
                            RequestCreateSection(epGroup.Key, chapterGroup.Key);
                        }
                        if (GUI.Button(btnRectMinus, "-", EditorStyles.miniButtonRight))
                        {
                            DeleteChapter(epGroup.Key, chapterGroup.Key);
                        }

                        // =========================================================

                        EditorGUI.indentLevel++;

                        var sections = chapterGroup.GroupBy(u => new
                        {
                            ID = u.SectionID,
                            NameKey = (u.SectionID == -1 ? u.SectionName : "")
                        }).OrderBy(g => g.Key.ID).ThenBy(g => g.Key.NameKey);

                        foreach (var sectionGroup in sections)
                        {
                            int secID = sectionGroup.Key.ID;
                            string secNameKey = sectionGroup.Key.NameKey;

                            if (!allowChapter && !sectionGroup.Any(u => u.ID.ToLower().Contains(_searchFilter.ToLower()))) continue;

                            var first = sectionGroup.FirstOrDefault();
                            string displayName = string.IsNullOrEmpty(first?.SectionName) ? (secID == -1 ? "Default" : secID.ToString()) : first.SectionName;

                            EditorGUILayout.BeginHorizontal();

                            GUILayout.Label("=", GUILayout.Width(20));
                            Rect handleRect = GUILayoutUtility.GetLastRect();

                            // =========================================================
                            // 【修正点2】ドラッグ開始ガードの徹底
                            // =========================================================
                            if (_currentDragData == null && evt.type == EventType.MouseDrag && handleRect.Contains(evt.mousePosition))
                            {
                                Debug.Log($"Drag Start Detected on Handle for {displayName}");

                                _currentDragData = new SectionDragData { Ep = epGroup.Key, Ch = chapterGroup.Key, Sec = secID };

                                DragAndDrop.PrepareStartDrag();
                                DragAndDrop.SetGenericData("SectionDrag", _currentDragData);
                                DragAndDrop.objectReferences = new UnityEngine.Object[0];
                                DragAndDrop.StartDrag($"Move Section {displayName}");
                                Event.current.Use();
                            }

                            if (GUILayout.Button($"{displayName} ({sectionGroup.Count()})", EditorStyles.miniButtonLeft))
                            {
                                string filterName = (secID == -1) ? secNameKey : null;
                                OnSelectSection?.Invoke(epGroup.Key, chapterGroup.Key, secID, filterName);
                                if (first != null) OnRequestPanTo?.Invoke(first.Position);
                            }

                            EditorGUILayout.EndHorizontal();

                            Rect btnRect = GUILayoutUtility.GetLastRect();
                            if (evt.type == EventType.MouseDown && evt.button == 1 && (btnRect.Contains(evt.mousePosition) || handleRect.Contains(evt.mousePosition)))
                            {
                                GenericMenu menu = new GenericMenu();
                                menu.AddItem(new GUIContent("Delete Section"), false, () => DeleteSection(epGroup.Key, chapterGroup.Key, secID));
                                menu.ShowAsContext();
                                Event.current.Use();
                            }
                        }
                        EditorGUI.indentLevel--;
                    }
                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space();
                }

                EditorGUILayout.Space();
                if (GUILayout.Button("Add Episode"))
                {
                    CreateEpisode();
                }
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();

            Rect divider = GUILayoutUtility.GetLastRect();
            divider.x += divider.width;
            divider.width = 1;
            EditorGUI.DrawRect(divider, Color.black);
        }

        // Helper Methods
        private void CreateChapter(int epID)
        {
            int nextCh = 1;
            if (Data.Conversations.Any(u => u.EpisodeID == epID))
            {
                nextCh = Data.Conversations.Where(u => u.EpisodeID == epID).Max(u => u.ChapterID) + 1;
            }
            int startSec = 1;
            string defaultName = GetUniqueSectionName(epID, nextCh);
            CreateSection(epID, nextCh, startSec, defaultName);
        }

        private void RequestCreateSection(int epID, int chapterID)
        {
            int nextSec = 1;
            if (Data.Conversations.Any(u => u.EpisodeID == epID && u.ChapterID == chapterID))
            {
                nextSec = Data.Conversations.Where(u => u.EpisodeID == epID && u.ChapterID == chapterID).Max(u => u.SectionID) + 1;
            }
            string defaultName = GetUniqueSectionName(epID, chapterID);
            CreateSection(epID, chapterID, nextSec, defaultName);
        }

        private string GetUniqueSectionName(int epID, int chapterID)
        {
            string baseName = "NewSection";
            int count = 1;
            string candidate = baseName;
            while (Data.Conversations.Any(u => u.EpisodeID == epID && u.ChapterID == chapterID && u.SectionName == candidate))
            {
                candidate = $"{baseName}_{count++}";
            }
            return candidate;
        }

        private void CreateSection(int epID, int chapterID, int sectionID, string sectionName)
        {
            var newNode = new ConversationUnit
            {
                ID = $"{epID}_{chapterID}_{sectionID}_1",
                EpisodeID = epID,
                ChapterID = chapterID,
                SectionID = sectionID,
                SectionName = sectionName,
                SpeakerName = "New Speaker",
                BodyText = "Start",
                Position = new Vector2(100, 100)
            };
            Data.Conversations.Add(newNode);
        }

        private void CreateEpisode()
        {
            int newEp = 1;
            if (Data.Conversations.Any())
            {
                newEp = Data.Conversations.Max(u => u.EpisodeID) + 1;
            }
            int ch = 1;
            RequestCreateSection(newEp, ch);
        }

        private void DeleteSection(int ep, int chapter, int section)
        {
            if (EditorUtility.DisplayDialog("Delete Section", $"Are you sure you want to delete section '{section}' in '{ep}/{chapter}'?", "Yes", "No"))
            {
                Data.Conversations.RemoveAll(u => u.EpisodeID == ep && u.ChapterID == chapter && u.SectionID == section);
            }
        }

        private void MoveSectionToChapter(int srcEp, int srcCh, int srcSec, int destEp, int destCh)
        {
            if (srcEp == destEp && srcCh == destCh) return;

            int newSecID = 1;
            var destUnits = Data.Conversations.Where(u => u.EpisodeID == destEp && u.ChapterID == destCh);
            if (destUnits.Any())
            {
                newSecID = destUnits.Max(u => u.SectionID) + 1;
            }

            var unitsToMove = Data.Conversations.Where(u => u.EpisodeID == srcEp && u.ChapterID == srcCh && u.SectionID == srcSec).ToList();
            foreach (var unit in unitsToMove)
            {
                unit.EpisodeID = destEp;
                unit.ChapterID = destCh;
                unit.SectionID = newSecID;
            }

            EditorUtility.SetDirty(Data);
            AssetDatabase.SaveAssets();
            Debug.Log($"Moved Section {srcSec} (from {srcEp}/{srcCh}) to {destEp}/{destCh} (New SectionID: {newSecID})");
        }

        private class SectionDragData { public int Ep; public int Ch; public int Sec; }

        private void DeleteChapter(int ep, int chapter)
        {
            if (EditorUtility.DisplayDialog("Delete Chapter", $"Are you sure you want to delete Chapter {chapter} in Episode {ep} and ALL its sections?", "Yes", "No"))
            {
                Data.Conversations.RemoveAll(u => u.EpisodeID == ep && u.ChapterID == chapter);
            }
        }
    }
}
