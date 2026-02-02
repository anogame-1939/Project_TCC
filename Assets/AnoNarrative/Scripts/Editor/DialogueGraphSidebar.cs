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

                        // Header Drop (Append to End of Chapter)
                        if (dropRect.Contains(evt.mousePosition))
                        {
                            if (_currentDragData != null)
                            {
                                if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
                                {
                                    DragAndDrop.visualMode = blockedByButton ? DragAndDropVisualMode.None : DragAndDropVisualMode.Move;

                                    if (evt.type == EventType.DragUpdated && !blockedByButton)
                                    {
                                        EditorGUI.DrawRect(dropRect, new Color(1f, 1f, 0f, 0.3f));
                                        Event.current.Use();
                                    }

                                    if (evt.type == EventType.DragPerform && !blockedByButton)
                                    {
                                        int maxSec = 0;
                                        if (chapterGroup.Any()) maxSec = chapterGroup.Max(u => u.SectionID);

                                        MoveSection(_currentDragData.Ep, _currentDragData.Ch, _currentDragData.Sec, epGroup.Key, chapterGroup.Key, maxSec + 1);

                                        DragAndDrop.AcceptDrag();
                                        _currentDragData = null;
                                        // DragAndDrop.PrepareStartDrag(); // Error fix
                                        Event.current.Use();
                                    }
                                }
                            }
                        }

                        // 3. ボタン描画（手動配置）
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
                        }).OrderBy(g => g.Key.ID).ThenBy(g => g.Key.NameKey).ToList();

                        for (int i = 0; i < sections.Count; i++)
                        {
                            var sectionGroup = sections[i];
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

                            // --- DRAG & DROP LOGIC (Ported from DragDropTestWindow) ---
                            Rect rowRect = GUILayoutUtility.GetLastRect();
                            // Ensure the rect covers the full width for easier catching
                            rowRect.x = 0;
                            rowRect.width = width;

                            float contentHeight = rowRect.height;
                            float spacing = 1f; // Define spacing here

                            // Define Drop Zones
                            // DropZone covers bottom half of this item + visual gap area. 
                            // Meaning if we drop here, we insert AFTER this item.
                            Rect dropZoneRect = new Rect(0, rowRect.y + (contentHeight * 0.5f), width, contentHeight);

                            // TopZone only for the very first item (insert at top)
                            Rect topZoneRect = new Rect(0, rowRect.y, width, contentHeight * 0.5f);

                            bool isInDropZone = false;
                            bool isInTopZone = false;

                            if (_currentDragData != null)
                            {
                                isInDropZone = dropZoneRect.Contains(evt.mousePosition);
                                isInTopZone = (i == 0) && topZoneRect.Contains(evt.mousePosition);

                                // 1. LOGIC PHASE (DragUpdated / DragPerform)
                                if (isInDropZone || isInTopZone)
                                {
                                    if (evt.type == EventType.DragUpdated)
                                    {
                                        DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                                        Event.current.Use();
                                        // Request Repaint to update the visuals in the next phase
                                        // (Since this is a helper class, we might need to rely on the window's repaint loop or trigger it)
                                        // For now, assuming the window calls Repaint on MouseMove/DragUpdated.
                                    }

                                    if (evt.type == EventType.DragPerform)
                                    {
                                        DragAndDrop.AcceptDrag();

                                        // Target Index Calculation
                                        // TopZone -> 0
                                        // DropZone -> i + 1 (Insert after current)
                                        int targetIndex = isInTopZone ? 0 : i + 1;

                                        PerformReorder(_currentDragData, epGroup.Key, chapterGroup.Key, targetIndex);

                                        _currentDragData = null;
                                        DragAndDrop.PrepareStartDrag();
                                        Event.current.Use();
                                    }
                                }
                            }

                            // 2. VISUAL PHASE (Draws during Repaint)
                            // Draw Cyan Line for feedback
                            if (_currentDragData != null && (evt.type == EventType.Repaint))
                            {
                                // We check global mouse position against our defined zones again for drawing
                                // (Or use the flags if we trust they are up to date from layout event, but Repaint is separate)
                                bool drawDrop = dropZoneRect.Contains(evt.mousePosition);
                                bool drawTop = (i == 0) && topZoneRect.Contains(evt.mousePosition);

                                if (drawDrop)
                                {
                                    // Line below the item
                                    float lineY = rowRect.yMax + (spacing * 0.5f);
                                    EditorGUI.DrawRect(new Rect(0, lineY - 1, width, 2), Color.cyan);
                                }
                                else if (drawTop)
                                {
                                    // Line above the first item
                                    EditorGUI.DrawRect(new Rect(0, rowRect.y - 1, width, 2), Color.cyan);
                                }
                            }

                            // Context Menu
                            Rect btnRect = rowRect;
                            if (evt.type == EventType.MouseDown && evt.button == 1 && btnRect.Contains(evt.mousePosition))
                            {
                                GenericMenu menu = new GenericMenu();
                                menu.AddItem(new GUIContent("Delete Section"), false, () => DeleteSection(epGroup.Key, chapterGroup.Key, secID));
                                menu.ShowAsContext();
                                Event.current.Use();
                            }

                            GUILayout.Space(spacing); // 実際にレイアウト上の隙間を空ける
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

        private void PerformReorder(SectionDragData dragData, int targetEp, int targetCh, int insertIndex)
        {
            // 同じアイテムへのドロップは無視
            if (dragData.Ep == targetEp && dragData.Ch == targetCh)
            {
                // 現在のリスト上のインデックスを取得
                var currentList = Data.Conversations
                    .Where(u => u.EpisodeID == targetEp && u.ChapterID == targetCh)
                    .GroupBy(u => u.SectionID) // Section単位
                    .OrderBy(g => g.Key)
                    .ToList();

                int currentIndex = currentList.FindIndex(g => g.Key == dragData.Sec);
                if (currentIndex == -1) return;

                // 同じ場所なら何もしない（微調整必要：下移動時のindexズレ考慮）
                if (currentIndex == insertIndex) return;
                if (currentIndex + 1 == insertIndex) return; // 自分の直下＝自分と同じ位置
            }

            // 1. 移動対象のUnitをすべて取得
            var movingUnits = Data.Conversations
                .Where(u => u.EpisodeID == dragData.Ep && u.ChapterID == dragData.Ch && u.SectionID == dragData.Sec)
                .ToList();

            // 2. 移動先チャプターの全セクションIDリストを作る（移動対象は除く）
            //    移動元と移動先が同じ場合、ここで除外されることで「抜けた状態」になる
            var targetChapterUnits = Data.Conversations
                .Where(u => u.EpisodeID == targetEp && u.ChapterID == targetCh)
                .Where(u => !(u.EpisodeID == dragData.Ep && u.ChapterID == dragData.Ch && u.SectionID == dragData.Sec)) // 移動対象を除外
                .GroupBy(u => u.SectionID)
                .OrderBy(g => g.Key)
                .ToList();

            // 3. insertIndexの位置に移動対象をダミーとして挿入したいが、
            //    GroupByのリストには直接入れられないので、IDリストを作る
            var newOrderSectionIDs = new List<int>();
            foreach (var g in targetChapterUnits) newOrderSectionIDs.Add(g.Key);

            // リスト内での挿入位置を調整
            // 同一グループ内移動の場合、移動元を除去したあとのインデックスに対して挿入位置が正しいか確認が必要
            // 上で「移動対象を除外」しているので、targetChapterUnitsは「抜けた後のリスト」になっている。
            // したがって insertIndex はそのまま「抜けた後のリストの何番目に挿入するか」として使えるが、
            // 下方向に移動した場合の補正が必要。
            // しかし、UIのループ(i)は「移動前」の状態で行われている。

            // 例: [A(0), B(1), C(2)] で A を B(1) の下 (=Index 2) に入れたい。
            // targetChapterUnits = [B, C]
            // insertIndex = 2. But targetChapterUnits.Count = 2. Insert(2) is OK (End).
            // Result: [B, C, A] -> Correct.

            // 例: [A(0), B(1), C(2)] で C を A(0) の上 (=Index 0) に入れたい。
            // targetChapterUnits = [A, B]
            // insertIndex = 0.
            // Result: [C, A, B] -> Correct.

            // 例: [A(0), B(1), C(2)] で A を A(0) の下 (=Index 1) に入れたい。(意味ない操作)
            // targetChapterUnits = [B, C]
            // insertIndex = 1.
            // Result: [B, A, C]. Wait, A was at 0, B at 1. Swapped.

            // UIのインデックス(i)を使って判断する場合、
            // 「自分より下の位置」に挿入する場合、自分自身がリストから消える分、インデックスが1つ減ることを考慮する。
            if (dragData.Ep == targetEp && dragData.Ch == targetCh)
            {
                var originalList = Data.Conversations
                    .Where(u => u.EpisodeID == targetEp && u.ChapterID == targetCh)
                    .GroupBy(u => u.SectionID).OrderBy(g => g.Key).ToList();
                int originalIndex = originalList.FindIndex(g => g.Key == dragData.Sec);

                if (insertIndex > originalIndex)
                {
                    insertIndex--;
                }
            }

            // 範囲制限
            if (insertIndex < 0) insertIndex = 0;
            if (insertIndex > targetChapterUnits.Count) insertIndex = targetChapterUnits.Count;

            // 4. 再配置＆ID書き換え実行
            // まず移動対象の所属を書き換え
            foreach (var unit in movingUnits)
            {
                unit.EpisodeID = targetEp;
                unit.ChapterID = targetCh;
                // SectionIDは後で決めるので一旦保留、あるいは仮
            }

            // targetChapterUnits (List<Group>) に movingUnits をインサートしたいが型が違う
            // 実体ではなく「順番」だけ確定させればいい。

            // 既存の要素（Group）のリスト
            var reorderedGroups = new List<List<ConversationUnit>>();
            foreach (var g in targetChapterUnits)
            {
                reorderedGroups.Add(g.ToList());
            }

            // 移動対象を挿入
            reorderedGroups.Insert(insertIndex, movingUnits);

            // 5. 連番振り直し
            int currentSecID = 1;
            foreach (var groupList in reorderedGroups)
            {
                foreach (var unit in groupList)
                {
                    unit.SectionID = currentSecID;
                }
                currentSecID++;
            }

            EditorUtility.SetDirty(Data);
            AssetDatabase.SaveAssets(); // 強制保存しないとID重複などでバグる可能性がある

            Debug.Log($"Reordered Section {dragData.Sec} to index {insertIndex} in {targetEp}/{targetCh}. New Max SecID: {currentSecID - 1}");
        }

        private void MoveSection(int srcEp, int srcCh, int srcSec, int destEp, int destCh, int destSec)
        {
            // 旧メソッド（互換性のため残すか、書き換えるか）
            // 今回は末尾追加用として使う

            // 移動
            var units = Data.Conversations.Where(u => u.EpisodeID == srcEp && u.ChapterID == srcCh && u.SectionID == srcSec).ToList();
            foreach (var unit in units)
            {
                unit.EpisodeID = destEp;
                unit.ChapterID = destCh;
                unit.SectionID = destSec;
            }
            EditorUtility.SetDirty(Data);
            AssetDatabase.SaveAssets();
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
