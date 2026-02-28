using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

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
        internal static bool IsActive;
        internal static event System.Action OnActiveChanged;

        // ── タグフィルタ（非表示タグセット） ──
        internal static readonly HashSet<Application.Event.EventColorTag> HiddenTags = new HashSet<Application.Event.EventColorTag>();

        // ── ランタイム座標記録 ──
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

        private const float SphereRadius = 1.5f;
        private const float HandleSize = 1.8f;
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
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!IsActive) return;

            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    _isPlayMode = true;
                    _runtimeMoves.Clear();
                    // 再プレイ時にポップアップを閉じる
                    var existing = EditorWindow.GetWindow<RuntimeMoveReviewWindow>(false, "", false);
                    if (existing != null) existing.Close();
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    _isPlayMode = false;
                    break;
                case PlayModeStateChange.EnteredEditMode:
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
            OnActiveChanged?.Invoke();
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

            // マウス移動でホバー判定を更新するため再描画を要求
            if (Event.current.type == EventType.MouseMove)
            {
                sceneView.Repaint();
            }

            var eventRoots = Object.FindObjectsByType<Application.Event.AnoEventRoot>(
                FindObjectsSortMode.None);

            // ── 最もカーソルに近い1つだけをホバー対象にする ──
            Application.Event.AnoEventRoot hoveredRoot = null;
            float closestDist = float.MaxValue;
            foreach (var root in eventRoots)
            {
                if (root == null) continue;
                if (HiddenTags.Contains(root.ColorTag)) continue;
                Vector3 pos = root.transform.position;
                float camSize = HandleUtility.GetHandleSize(pos) * 0.8f;
                float hSize = Mathf.Max(SphereRadius, camSize);
                float dist = HandleUtility.DistanceToCircle(pos, hSize);
                if (dist <= 0f && dist < closestDist)
                {
                    closestDist = dist;
                    hoveredRoot = root;
                }
            }

            // ── ホバー中のオブジェクトをクリックしたらSelectionに設定 ──
            if (hoveredRoot != null
                && Event.current.type == EventType.MouseDown
                && Event.current.button == 0)
            {
                Selection.activeGameObject = hoveredRoot.gameObject;
                EditorGUIUtility.PingObject(hoveredRoot.gameObject);
            }

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
            float cameraBasedSize = HandleUtility.GetHandleSize(pos) * 0.8f;
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

            // ── 選択ハイライト ──
            bool isSelected = Selection.activeGameObject == eventRoot.gameObject;
            if (isSelected)
            {
                Handles.color = Color.white;
                Handles.DrawWireDisc(pos, Vector3.up, SphereRadius + 0.2f, 3f);
                Handles.DrawWireDisc(pos, Vector3.forward, SphereRadius + 0.2f, 3f);
                Handles.DrawWireDisc(pos, Vector3.right, SphereRadius + 0.2f, 3f);
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

            // ── FreeMoveHandle ──
            Handles.color = handleColor;

            EditorGUI.BeginChangeCheck();
            Vector3 newPos = Handles.FreeMoveHandle(
                pos,
                handleSize,
                Vector3.one * 0.5f,
                Handles.SphereHandleCap);

            if (EditorGUI.EndChangeCheck())
            {
                // Y=0 固定
                newPos.y = 0f;

                // PlayMode中なら座標を記録
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

                // インスペクターに表示 + ヒエラルキーでハイライト
                Selection.activeGameObject = eventRoot.gameObject;
                EditorGUIUtility.PingObject(eventRoot.gameObject);
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
                var allRoots = Object.FindObjectsByType<Application.Event.AnoEventRoot>(FindObjectsSortMode.None);
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
    [Overlay(typeof(SceneView), "event-root-visualizer-overlay", "EventRoot")]
    public class AnoEventRootVisualizerOverlay : Overlay
    {
        private Toggle _toggle;

        public override VisualElement CreatePanelContent()
        {
            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;
            root.style.alignItems = Align.Center;
            root.style.paddingLeft = 4;
            root.style.paddingRight = 4;
            root.style.paddingTop = 2;
            root.style.paddingBottom = 2;
            root.style.width = 120;

            _toggle = new Toggle();
            _toggle.SetValueWithoutNotify(AnoEventRootVisualizer.IsActive);
            _toggle.RegisterValueChangedCallback(evt =>
            {
                AnoEventRootVisualizer.SetActive(evt.newValue);
            });
            _toggle.style.marginRight = 6;
            root.Add(_toggle);

            var label = new Label(AnoEventRootVisualizer.IsActive
                ? "Active"
                : "Inactive");
            label.style.unityFontStyleAndWeight = FontStyle.Normal;
            root.Add(label);

            AnoEventRootVisualizer.OnActiveChanged += () =>
            {
                _toggle?.SetValueWithoutNotify(AnoEventRootVisualizer.IsActive);
                label.text = AnoEventRootVisualizer.IsActive
                    ? "Active"
                    : "Inactive";
            };

            return root;
        }
    }
}
