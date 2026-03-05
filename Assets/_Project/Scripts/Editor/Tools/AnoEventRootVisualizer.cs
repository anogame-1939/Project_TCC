using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;
using AnoGame.Editor.EventPlacement;

namespace AnoGame.Editor.Tools
{
    /// <summary>
    /// AnoEventRoot を持つオブジェクトをシーン上で可視化するツール。
    /// 球体ギズモ + ラベル + FreeMoveHandle で大判定移動を実現。
    /// </summary>
    [InitializeOnLoad]
    public static class AnoEventRootVisualizer
    {
        // ── 状態 ──
        private const string PrefKeyIsActive = "AnoEventRootVisualizer_IsActive";
        internal static bool IsActive;
        internal static event Action OnActiveChanged;
        internal static event Action OnSceneSelectionChanged;

        // ── 配置モード ──
        internal static bool IsPlacementMode;
        internal static ReceptorType PlacementReceptorType;
        internal static event Action OnPlacementModeChanged;

        // ── タグフィルタ（非表示タグセット） ──
        internal static readonly HashSet<Application.Event.EventColorTag> HiddenTags = new HashSet<Application.Event.EventColorTag>();

        // ── 複数選択対象（Dashboardから設定） ──
        internal static readonly HashSet<Application.Event.AnoEventRoot> SelectedRoots = new HashSet<Application.Event.AnoEventRoot>();

        // ── ランタイム座標記録 ──
        private const string SessionKeyRuntimeMoves = "AnoEventRootVisualizer_RuntimeMoves";
        private static readonly Dictionary<int, RuntimeMoveRecord> _runtimeMoves = new Dictionary<int, RuntimeMoveRecord>();
        private static bool _isPlayMode;

        public struct RuntimeMoveRecord
        {
            public string Name;
            public string ScenePath;
            public Vector3 OriginalPosition;
            public Vector3 NewPosition;
        }

        // ── 描画設定 ──
        private static readonly Color SphereColor = new Color(0.2f, 0.8f, 0.4f, 0.25f);
        private static readonly Color SphereOutlineColor = new Color(0.2f, 0.8f, 0.4f, 0.8f);
        private static readonly Color LabelBgColor = new Color(0f, 0f, 0f, 0.7f);
        private static readonly Color LabelTextColor = new Color(0.9f, 1f, 0.9f, 1f);
        private static readonly Color HandleColor = new Color(0.3f, 0.9f, 0.5f, 0.6f);

        private const float SphereRadius = 1.3f;
        private const float LabelOffsetY = 2.5f;

        // ── 名前整形用正規表現 ──
        // "EV_005_Get_Magnet_磁石入手" → "EV_005_磁石入手"
        // "~EV_001_BigManEncounter_大男と初回遭遇" → "EV_001_大男と初回遭遇"
        // パターン: 先頭の~ を除去、EV_数字_ を保持、最後の日本語部分を抽出
        private static readonly Regex NameFormatRegex = new Regex(
            @"^~?(EV_\d+)_.*?([^\x00-\x7F].*)$",
            RegexOptions.Compiled);

        // ── メニュー ──
        private const string MenuPath = "Tools/AnoEventRoot Visualizer";

        static AnoEventRootVisualizer()
        {
            IsActive = EditorPrefs.GetBool(PrefKeyIsActive, false);
            RestoreRuntimeMoves();
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    _isPlayMode = true;
                    _runtimeMoves.Clear();
                    SaveRuntimeMoves();
                    // 再プレイ時にポップアップを閉じる
                    var existing = EditorWindow.GetWindow<RuntimeMoveReviewWindow>(false, "", false);
                    if (existing != null) existing.Close();
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    _isPlayMode = false;
                    // ドメインリロード前に記録を永続化
                    SaveRuntimeMoves();
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    // ドメインリロード後に復元された記録も含めて表示
                    RestoreRuntimeMoves();
                    if (_runtimeMoves.Count > 0)
                    {
                        RuntimeMoveReviewWindow.Show(_runtimeMoves);
                    }
                    break;
            }
        }

        /// <summary>
        /// PlayMode中の移動を記録する
        /// </summary>
        private static void RecordRuntimeMove(Application.Event.AnoEventRoot eventRoot, Vector3 originalPos, Vector3 newPos)
        {
            int id = eventRoot.gameObject.GetInstanceID();
            if (!_runtimeMoves.ContainsKey(id))
            {
                _runtimeMoves[id] = new RuntimeMoveRecord
                {
                    Name = eventRoot.gameObject.name,
                    ScenePath = GetGameObjectPath(eventRoot.transform),
                    OriginalPosition = originalPos,
                    NewPosition = newPos
                };
            }
            else
            {
                var record = _runtimeMoves[id];
                record.NewPosition = newPos;
                _runtimeMoves[id] = record;
            }
            SaveRuntimeMoves();
        }

        private static string GetGameObjectPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }

        // ── SessionState 永続化 ──

        [Serializable]
        private class RuntimeMoveRecordSerializable
        {
            public int Key;
            public string Name;
            public string ScenePath;
            public float OrigX, OrigY, OrigZ;
            public float NewX, NewY, NewZ;
        }

        [Serializable]
        private class RuntimeMoveRecordList
        {
            public List<RuntimeMoveRecordSerializable> Items = new List<RuntimeMoveRecordSerializable>();
        }

        private static void SaveRuntimeMoves()
        {
            var list = new RuntimeMoveRecordList();
            foreach (var kvp in _runtimeMoves)
            {
                list.Items.Add(new RuntimeMoveRecordSerializable
                {
                    Key = kvp.Key,
                    Name = kvp.Value.Name,
                    ScenePath = kvp.Value.ScenePath,
                    OrigX = kvp.Value.OriginalPosition.x,
                    OrigY = kvp.Value.OriginalPosition.y,
                    OrigZ = kvp.Value.OriginalPosition.z,
                    NewX = kvp.Value.NewPosition.x,
                    NewY = kvp.Value.NewPosition.y,
                    NewZ = kvp.Value.NewPosition.z
                });
            }
            string json = JsonUtility.ToJson(list);
            SessionState.SetString(SessionKeyRuntimeMoves, json);
        }

        private static void RestoreRuntimeMoves()
        {
            string json = SessionState.GetString(SessionKeyRuntimeMoves, "");
            if (string.IsNullOrEmpty(json)) return;

            var list = JsonUtility.FromJson<RuntimeMoveRecordList>(json);
            if (list == null || list.Items == null) return;

            _runtimeMoves.Clear();
            foreach (var item in list.Items)
            {
                _runtimeMoves[item.Key] = new RuntimeMoveRecord
                {
                    Name = item.Name,
                    ScenePath = item.ScenePath,
                    OriginalPosition = new Vector3(item.OrigX, item.OrigY, item.OrigZ),
                    NewPosition = new Vector3(item.NewX, item.NewY, item.NewZ)
                };
            }
        }

        [MenuItem(MenuPath, false, 201)]
        private static void ToggleMode()
        {
            SetActive(!IsActive);
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleModeValidate()
        {
            Menu.SetChecked(MenuPath, IsActive);
            return true;
        }

        internal static void SetActive(bool active)
        {
            IsActive = active;
            EditorPrefs.SetBool(PrefKeyIsActive, active);
            if (!active) SetPlacementMode(false, ReceptorType.Inspect);
            OnActiveChanged?.Invoke();
            SceneView.RepaintAll();
        }

        internal static void SetPlacementMode(bool active, ReceptorType type)
        {
            IsPlacementMode = active;
            PlacementReceptorType = type;
            OnPlacementModeChanged?.Invoke();
            SceneView.RepaintAll();
        }

        /// <summary>
        /// オブジェクト名を整形して表示用ラベルを生成する。
        /// "EV_005_Get_Magnet_磁石入手" → "EV_005_磁石入手"
        /// "~EV_001_BigManEncounter_大男と初回遭遇" → "EV_001_大男と初回遭遇"
        /// </summary>
        private static string FormatDisplayName(string objectName)
        {
            var match = NameFormatRegex.Match(objectName);
            if (match.Success && match.Groups.Count >= 3)
            {
                string prefix = match.Groups[1].Value; // "EV_005"
                string japanesePart = match.Groups[2].Value; // "磁石入手"
                if (!string.IsNullOrEmpty(japanesePart))
                {
                    return prefix + "_" + japanesePart;
                }
            }
            // フォールバック: ~を除去して返す
            return objectName.TrimStart('~');
        }

        // ====================================================================
        // SceneView 描画
        // ====================================================================
        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!IsActive) return;

            // 破棄済みオブジェクトを除去
            SelectedRoots.RemoveWhere(r => r == null);

            // マウス移動でホバー判定を更新するため再描画を要求
            if (Event.current.type == EventType.MouseMove)
            {
                sceneView.Repaint();
            }

            // ── 配置モード処理 ──
            if (IsPlacementMode)
            {
                HandlePlacementMode(sceneView);
                // 配置モード中も既存ギズモを描画（参考用）
                DrawAllEventRoots(null);
                return;
            }

            var eventRoots = UnityEngine.Object.FindObjectsByType<Application.Event.AnoEventRoot>(
                FindObjectsSortMode.None);

            // ── 最もカーソルに近い1つだけをホバー対象にする ──
            Application.Event.AnoEventRoot hoveredRoot = null;
            float closestDist = float.MaxValue;
            foreach (var root in eventRoots)
            {
                if (root == null) continue;
                if (HiddenTags.Contains(root.ColorTag)) continue;
                Vector3 pos = root.transform.position;
                float camSize = HandleUtility.GetHandleSize(pos) * 0.65f;
                float hSize = Mathf.Max(SphereRadius, camSize);
                float dist = HandleUtility.DistanceToCircle(pos, hSize);
                if (dist <= 0f && dist < closestDist)
                {
                    closestDist = dist;
                    hoveredRoot = root;
                }
            }

            // ── ホバー中のオブジェクトをクリックしたらSelectionに設定 ──
            bool isModifierClick = Event.current.control || Event.current.shift;
            if (hoveredRoot != null
                && Event.current.type == EventType.MouseDown
                && Event.current.button == 0
                && isModifierClick)
            {
                // Ctrl/Shift+クリック: トグル追加/解除
                bool alreadySelected = SelectedRoots.Contains(hoveredRoot);
                if (alreadySelected)
                    SelectedRoots.Remove(hoveredRoot);
                else
                    SelectedRoots.Add(hoveredRoot);

                Selection.activeGameObject = hoveredRoot.gameObject;
                OnSceneSelectionChanged?.Invoke();

                // FreeMoveHandleへの伝撬を防止
                Event.current.Use();
                return;
            }

            // 通常クリック（修飾キーなし）: FreeMoveHandleに任せるが、
            // ホバー先が未選択なら単独選択に切替
            if (hoveredRoot != null
                && Event.current.type == EventType.MouseDown
                && Event.current.button == 0
                && !isModifierClick)
            {
                if (!SelectedRoots.Contains(hoveredRoot))
                {
                    SelectedRoots.Clear();
                    SelectedRoots.Add(hoveredRoot);
                    OnSceneSelectionChanged?.Invoke();
                    Selection.activeGameObject = hoveredRoot.gameObject;
                }
                // 選択済みオブジェクトの場合は Selection を変更しない
                // （変更すると OnUnitySelectionChanged 経由で SelectedRoots がリセットされるため）
            }

            foreach (var root in eventRoots)
            {
                if (root == null) continue;
                if (HiddenTags.Contains(root.ColorTag)) continue;
                DrawEventRootGizmo(root, root == hoveredRoot);
            }
        }

        // ====================================================================
        // 配置モード
        // ====================================================================

        private static void HandlePlacementMode(SceneView sceneView)
        {
            // Escape or 右クリック → 配置モード解除
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                SetPlacementMode(false, PlacementReceptorType);
                Event.current.Use();
                return;
            }
            if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
            {
                SetPlacementMode(false, PlacementReceptorType);
                Event.current.Use();
                return;
            }

            // マウス位置をワールド座標に変換（レイキャスト）
            Vector3 worldPos = Vector3.zero;
            bool hasHit = false;

            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                worldPos = hit.point;
                worldPos.y = 0f;
                hasHit = true;
            }
            else
            {
                // コライダーがない場合は Y=0 平面との交点を使用
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                if (groundPlane.Raycast(ray, out float distance))
                {
                    worldPos = ray.GetPoint(distance);
                    worldPos.y = 0f;
                    hasHit = true;
                }
            }

            // プレビュー描画
            if (hasHit)
            {
                // 薄い球体プレビュー
                Color previewColor = new Color(1f, 0.8f, 0.2f, 0.3f);
                Handles.color = previewColor;
                Handles.SphereHandleCap(0, worldPos, Quaternion.identity, SphereRadius * 2f, EventType.Repaint);

                // ワイヤフレーム
                Handles.color = new Color(1f, 0.8f, 0.2f, 0.8f);
                Handles.DrawWireDisc(worldPos, Vector3.up, SphereRadius, 2f);

                // 配置先フォルダとID情報をラベル表示
                string folderPath = EventPlacementService.GetActiveFolderPath();
                string nextId = EventPlacementService.GenerateNextEventId(folderPath);
                string previewLabel = $"{nextId} [{PlacementReceptorType}]";
                DrawLabel(worldPos + Vector3.up * LabelOffsetY, previewLabel, new Color(0.8f, 0.6f, 0f, 0.85f));
            }

            // 左クリック → 配置実行
            if (hasHit && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                EventPlacementService.PlaceEvent(worldPos, PlacementReceptorType);
                Event.current.Use();
                // 連続配置: モードは維持
            }

            // SceneView の既定の選択動作を抑制
            if (Event.current.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            }

            // ステータスバーにガイド表示
            Handles.BeginGUI();
            var statusRect = new Rect(10, sceneView.position.height - 60, 400, 24);
            EditorGUI.DrawRect(statusRect, new Color(0, 0, 0, 0.7f));
            GUI.Label(statusRect, $"  [配置モード] {PlacementReceptorType} -- 左クリック: 配置 / Esc,右クリック: 解除",
                new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = new Color(1f, 0.9f, 0.3f) }, alignment = TextAnchor.MiddleLeft });
            Handles.EndGUI();
        }

        /// <summary>
        /// 全 AnoEventRoot のギズモを描画する（配置モード中のバックグラウンド描画用）
        /// </summary>
        private static void DrawAllEventRoots(Application.Event.AnoEventRoot hoveredRoot)
        {
            var eventRoots = UnityEngine.Object.FindObjectsByType<Application.Event.AnoEventRoot>(
                FindObjectsSortMode.None);
            foreach (var root in eventRoots)
            {
                if (root == null) continue;
                if (HiddenTags.Contains(root.ColorTag)) continue;
                DrawEventRootGizmo(root, root == hoveredRoot);
            }
        }

        private static void DrawEventRootGizmo(Application.Event.AnoEventRoot eventRoot, bool isHovered)
        {
            Transform t = eventRoot.transform;
            Vector3 pos = t.position;

            // タグ色を取得
            Color tagColor = Application.Event.AnoEventRoot.GetTagColor(eventRoot.ColorTag);

            // ── ハンドルサイズ算出（ホバー判定とFreeMoveHandleで共通） ──
            float cameraBasedSize = HandleUtility.GetHandleSize(pos) * 0.65f;
            float handleSize = Mathf.Max(SphereRadius, cameraBasedSize);

            // ホバー時は黄色っぽいハイライト
            Color sphereColor;
            Color outlineColor;
            if (isHovered)
            {
                sphereColor = new Color(1f, 0.9f, 0.3f, 0.5f);
                outlineColor = new Color(1f, 0.9f, 0.3f, 1f);
            }
            else
            {
                sphereColor = new Color(tagColor.r, tagColor.g, tagColor.b, 0.25f);
                outlineColor = new Color(tagColor.r, tagColor.g, tagColor.b, 0.8f);
            }
            Color handleColor = new Color(tagColor.r, tagColor.g, tagColor.b, 0.6f);

            // ── 選択ハイライト（複数選択対応） ──
            bool isSelected = SelectedRoots.Contains(eventRoot)
                || Selection.activeGameObject == eventRoot.gameObject;
            if (isSelected)
            {
                Handles.color = Color.white;
                Handles.DrawWireDisc(pos, Vector3.up, SphereRadius, 3f);
                Handles.DrawWireDisc(pos, Vector3.forward, SphereRadius, 3f);
                Handles.DrawWireDisc(pos, Vector3.right, SphereRadius, 3f);
            }

            // ── 球体 ──
            Handles.color = sphereColor;
            float drawRadius = isHovered ? handleSize : SphereRadius;
            Handles.SphereHandleCap(0, pos, Quaternion.identity,
                drawRadius * 2f, EventType.Repaint);

            // ── 球体ワイヤフレーム ──
            Handles.color = outlineColor;
            float wireThickness = isHovered ? 3f : 1f;
            Handles.DrawWireDisc(pos, Vector3.up, drawRadius, wireThickness);
            Handles.DrawWireDisc(pos, Vector3.forward, drawRadius, wireThickness);
            Handles.DrawWireDisc(pos, Vector3.right, drawRadius, wireThickness);

            // ── FreeMoveHandle（Ctrl押下中はスキップ＝選択モード専用） ──
            if (!Event.current.control)
            {
                Handles.color = handleColor;

                EditorGUI.BeginChangeCheck();
                Vector3 newPos = Handles.FreeMoveHandle(
                    pos,
                    handleSize,
                    Vector3.one * 0.5f,
                    Handles.SphereHandleCap);

                if (EditorGUI.EndChangeCheck())
                {
                    Vector3 delta = newPos - pos;
                    delta.y = 0f; // Y=0 固定

                    // 複数選択時は全対象を一括移動
                    if (SelectedRoots.Count > 1 && SelectedRoots.Contains(eventRoot))
                    {
                        foreach (var root in SelectedRoots)
                        {
                            if (root == null) continue;
                            var rt = root.transform;
                            Vector3 target = rt.position + delta;
                            target.y = 0f;

                            if (_isPlayMode)
                            {
                                RecordRuntimeMove(root, rt.position, target);
                            }
                            else
                            {
                                Undo.RecordObject(rt, "Move AnoEventRoot (Bulk)");
                            }

                            rt.position = target;

                            if (!_isPlayMode)
                            {
                                EditorUtility.SetDirty(rt);
                            }
                        }
                    }
                    else
                    {
                        // 単体移動
                        newPos.y = 0f;

                        if (_isPlayMode)
                        {
                            RecordRuntimeMove(eventRoot, pos, newPos);
                        }
                        else
                        {
                            Undo.RecordObject(t, "Move AnoEventRoot");
                        }

                        t.position = newPos;

                        if (!_isPlayMode)
                        {
                            EditorUtility.SetDirty(t);
                        }
                    }

                    // 単体移動時のみインスペクターに表示
                    if (SelectedRoots.Count <= 1)
                    {
                        Selection.activeGameObject = eventRoot.gameObject;
                    }
                }
            }

            // ── ラベル表示 ──
            string displayName = FormatDisplayName(eventRoot.gameObject.name);
            DrawLabel(pos + Vector3.up * LabelOffsetY, displayName, isSelected ? outlineColor : LabelBgColor);
        }

        private static void DrawLabel(Vector3 worldPos, string text, Color bgColor)
        {
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = LabelTextColor },
                padding = new RectOffset(6, 6, 2, 2)
            };

            // 背景付きラベル
            Handles.BeginGUI();
            Vector2 screenPos = HandleUtility.WorldToGUIPoint(worldPos);
            Vector2 textSize = style.CalcSize(new GUIContent(text));
            Rect bgRect = new Rect(
                screenPos.x - textSize.x * 0.5f - 4,
                screenPos.y - textSize.y * 0.5f - 2,
                textSize.x + 8,
                textSize.y + 4);

            EditorGUI.DrawRect(bgRect, bgColor);
            GUI.Label(bgRect, text, style);
            Handles.EndGUI();
        }
    }

    // ========================================================================
    // ランタイム移動レビューウィンドウ
    // ========================================================================
    public class RuntimeMoveReviewWindow : EditorWindow
    {
        private Dictionary<int, AnoEventRootVisualizer.RuntimeMoveRecord> _records;
        private Vector2 _scrollPosition;

        public static void Show(Dictionary<int, AnoEventRootVisualizer.RuntimeMoveRecord> records)
        {
            var window = GetWindow<RuntimeMoveReviewWindow>(true, "EventRoot 移動一覧");
            window._records = new Dictionary<int, AnoEventRootVisualizer.RuntimeMoveRecord>(records);
            window.minSize = new Vector2(420, 200);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            // 再プレイ時に自動クローズ
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                Close();
            }
        }

        private void OnGUI()
        {
            if (_records == null || _records.Count == 0)
            {
                EditorGUILayout.LabelField("移動された EventRoot はありません。");
                return;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(
                $"Play中に移動されたEventRoot: {_records.Count}件",
                EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            var keysToRemove = new List<int>();

            foreach (var kvp in _records)
            {
                var record = kvp.Value;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.LabelField(record.Name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    $"  {record.OriginalPosition:F2}  >>  {record.NewPosition:F2}");

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Update", GUILayout.Width(80)))
                {
                    ApplyPosition(record);
                    keysToRemove.Add(kvp.Key);
                }

                if (GUILayout.Button("Skip", GUILayout.Width(60)))
                {
                    keysToRemove.Add(kvp.Key);
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.EndScrollView();

            foreach (var key in keysToRemove)
            {
                _records.Remove(key);
            }

            if (_records.Count == 0)
            {
                Close();
                return;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("全てUpdate", GUILayout.Width(120), GUILayout.Height(30)))
            {
                foreach (var kvp in _records)
                {
                    ApplyPosition(kvp.Value);
                }
                _records.Clear();
                Close();
            }

            if (GUILayout.Button("全てSkip", GUILayout.Width(100), GUILayout.Height(30)))
            {
                _records.Clear();
                Close();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
        }

        private void ApplyPosition(AnoEventRootVisualizer.RuntimeMoveRecord record)
        {
            var go = GameObject.Find(record.ScenePath);
            if (go == null)
            {
                // パスで見つからない場合、名前で検索
                var allRoots = UnityEngine.Object.FindObjectsByType<Application.Event.AnoEventRoot>(FindObjectsSortMode.None);
                foreach (var root in allRoots)
                {
                    if (root.gameObject.name == record.Name)
                    {
                        go = root.gameObject;
                        break;
                    }
                }
            }

            if (go != null)
            {
                Undo.RecordObject(go.transform, "Apply Runtime Position");
                go.transform.position = record.NewPosition;
                EditorUtility.SetDirty(go.transform);
                Debug.Log($"[EventRoot] {record.Name} の座標を更新: {record.NewPosition}");
            }
            else
            {
                Debug.LogWarning($"[EventRoot] {record.Name} が見つかりませんでした。");
            }
        }
    }

    // ========================================================================
    // SceneView Overlay — EventRoot Visualizer フローティングパネル
    // ========================================================================
    [Overlay(typeof(SceneView), "event-root-visualizer-overlay", "EventRoot", defaultDisplay = true)]
    public class AnoEventRootVisualizerOverlay : Overlay
    {
        private Toggle _toggle;
        private Button _placementBtn;
        private Label _statusLabel;
        private VisualElement _receptorPanel;
        private const string PrefKey = "EventRoot_Overlay_Init_v2";

        public override void OnCreated()
        {
            base.OnCreated();
            if (!EditorPrefs.GetBool(PrefKey, false))
            {
                displayed = true;
                EditorPrefs.SetBool(PrefKey, true);
            }
        }

        public override VisualElement CreatePanelContent()
        {
            var root = new VisualElement();
            root.style.paddingLeft = 4;
            root.style.paddingRight = 4;
            root.style.paddingTop = 2;
            root.style.paddingBottom = 2;
            root.style.minWidth = 140;

            // ── Row 1: Active Toggle ──
            var row1 = new VisualElement();
            row1.style.flexDirection = FlexDirection.Row;
            row1.style.alignItems = Align.Center;

            _toggle = new Toggle();
            _toggle.SetValueWithoutNotify(AnoEventRootVisualizer.IsActive);
            _toggle.RegisterValueChangedCallback(evt =>
            {
                AnoEventRootVisualizer.SetActive(evt.newValue);
            });
            _toggle.style.marginRight = 6;
            row1.Add(_toggle);

            var label = new Label(AnoEventRootVisualizer.IsActive
                ? "Active"
                : "Inactive");
            label.style.unityFontStyleAndWeight = FontStyle.Normal;
            row1.Add(label);

            root.Add(row1);

            // ── Row 2: Placement Button ──
            _placementBtn = new Button() { text = "+ Event" };
            _placementBtn.tooltip = "クリックして Receptor 種別を選択し、シーン上をクリックしてイベントを配置";
            _placementBtn.style.height = 22;
            _placementBtn.style.marginTop = 4;
            _placementBtn.clicked += OnPlacementButtonClicked;
            _placementBtn.SetEnabled(AnoEventRootVisualizer.IsActive);
            root.Add(_placementBtn);

            // ── Row 3: Receptor Type Selection Panel (initially hidden) ──
            _receptorPanel = new VisualElement();
            _receptorPanel.style.display = DisplayStyle.None;
            _receptorPanel.style.marginTop = 2;
            _receptorPanel.style.paddingTop = 2;
            _receptorPanel.style.paddingBottom = 2;
            _receptorPanel.style.borderTopWidth = 1;
            _receptorPanel.style.borderTopColor = new StyleColor(new Color(0.4f, 0.4f, 0.4f));

            var contactBtn = new Button(() => StartPlacement(ReceptorType.Contact))
            { text = "Contact" };
            contactBtn.tooltip = "距離トリガー";
            contactBtn.style.height = 20;
            contactBtn.style.fontSize = 11;
            contactBtn.style.marginTop = 1;
            contactBtn.style.marginBottom = 1;
            _receptorPanel.Add(contactBtn);

            var inspectBtn = new Button(() => StartPlacement(ReceptorType.Inspect))
            { text = "Inspect" };
            inspectBtn.tooltip = "調べる";
            inspectBtn.style.height = 20;
            inspectBtn.style.fontSize = 11;
            inspectBtn.style.marginTop = 1;
            inspectBtn.style.marginBottom = 1;
            _receptorPanel.Add(inspectBtn);

            var itemBtn = new Button(() => StartPlacement(ReceptorType.Item))
            { text = "Item" };
            itemBtn.tooltip = "アイテム使用";
            itemBtn.style.height = 20;
            itemBtn.style.fontSize = 11;
            itemBtn.style.marginTop = 1;
            itemBtn.style.marginBottom = 1;
            _receptorPanel.Add(itemBtn);

            root.Add(_receptorPanel);

            // ── Row 4: Status Label ──
            _statusLabel = new Label("");
            _statusLabel.style.fontSize = 10;
            _statusLabel.style.color = new StyleColor(new Color(1f, 0.85f, 0.3f));
            _statusLabel.style.marginTop = 2;
            _statusLabel.style.display = DisplayStyle.None;
            root.Add(_statusLabel);

            // ── イベント購読 ──
            AnoEventRootVisualizer.OnActiveChanged += () =>
            {
                _toggle?.SetValueWithoutNotify(AnoEventRootVisualizer.IsActive);
                label.text = AnoEventRootVisualizer.IsActive
                    ? "Active"
                    : "Inactive";
                _placementBtn?.SetEnabled(AnoEventRootVisualizer.IsActive);
            };

            AnoEventRootVisualizer.OnPlacementModeChanged += () =>
            {
                UpdatePlacementUI();
            };

            return root;
        }

        private void OnPlacementButtonClicked()
        {
            if (AnoEventRootVisualizer.IsPlacementMode)
            {
                // 配置モード解除
                AnoEventRootVisualizer.SetPlacementMode(false, ReceptorType.Inspect);
                return;
            }

            // 種別選択パネルの表示/非表示をトグル
            if (_receptorPanel != null)
            {
                bool visible = _receptorPanel.style.display == DisplayStyle.Flex;
                _receptorPanel.style.display = visible ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        private void StartPlacement(ReceptorType type)
        {
            // 種別パネルを閉じる
            if (_receptorPanel != null)
                _receptorPanel.style.display = DisplayStyle.None;

            AnoEventRootVisualizer.SetPlacementMode(true, type);
        }

        private void UpdatePlacementUI()
        {
            if (_placementBtn == null || _statusLabel == null) return;

            if (AnoEventRootVisualizer.IsPlacementMode)
            {
                _placementBtn.text = "x Stop";
                _placementBtn.style.backgroundColor = new StyleColor(new Color(0.7f, 0.3f, 0.2f, 0.8f));
                _statusLabel.text = $"[{AnoEventRootVisualizer.PlacementReceptorType}]";
                _statusLabel.style.display = DisplayStyle.Flex;
            }
            else
            {
                _placementBtn.text = "+ Event";
                _placementBtn.style.backgroundColor = StyleKeyword.Null;
                _statusLabel.style.display = DisplayStyle.None;
            }
        }
    }
}
