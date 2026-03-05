using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnoGame.Editor.Tools
{
    /// <summary>
    /// シーン上にピンを立て、ヒエラルキーのボタンからオブジェクトをピン位置に移動するツール。
    /// </summary>
    [InitializeOnLoad]
    public static class ScenePinPlacementTool
    {
        // ── 状態 ──
        internal static bool IsActive;
        private static Vector3? _pinPosition;

        // ── 描画設定 ──
        private static readonly Color PinBaseColor = new Color(1f, 0.85f, 0f, 0.6f);
        private static readonly Color PinShaftColor = new Color(0.9f, 0.2f, 0.2f, 1f);
        private static readonly Color PinHeadColor = new Color(0.9f, 0.15f, 0.15f, 1f);
        private static readonly Color CloseButtonColor = new Color(0.8f, 0.1f, 0.1f, 0.9f);
        private static readonly Color HierarchyButtonColor = new Color(0.2f, 0.7f, 0.3f, 1f);

        private const float BaseDiskRadius = 0.3f;
        private const float ShaftHeight = 2.0f;
        private const float HeadRadius = 0.2f;
        private const float CloseButtonRadius = 0.15f;
        private const float CloseButtonOffsetX = 0.6f;
        private const float CloseButtonOffsetY = 0.4f;

        // ── メニュー ──
        private const string MenuPath = "Tools/Scene Pin Placement";

        // ── Overlay 更新用 ──
        internal static event System.Action OnActiveChanged;

        static ScenePinPlacementTool()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyGUI;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        }

        [MenuItem(MenuPath, false, 200)]
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
            if (!IsActive)
            {
                _pinPosition = null;
            }
            OnActiveChanged?.Invoke();
            SceneView.RepaintAll();
            EditorApplication.RepaintHierarchyWindow();
        }

        // ====================================================================
        // SceneView 描画 + クリック検知
        // ====================================================================
        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!IsActive) return;

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            // ── クリック検知 ──
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

                // 常に Y=0 平面との交差のみ使用（コライダー無視）
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                if (groundPlane.Raycast(ray, out float distance))
                {
                    Vector3 hitPoint = ray.GetPoint(distance);
                    _pinPosition = new Vector3(hitPoint.x, 0f, hitPoint.z);

                    e.Use();
                    SceneView.RepaintAll();
                    EditorApplication.RepaintHierarchyWindow();
                }
            }

            // ── ピン描画 ──
            if (_pinPosition.HasValue)
            {
                DrawPin(_pinPosition.Value);
                DrawCloseButton(_pinPosition.Value);
            }

            // ── モード表示ラベル ──
            Handles.BeginGUI();
            var labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal = { textColor = PinHeadColor }
            };
            GUI.Label(new Rect(10, 10, 300, 30), "-- Pin Placement Mode --", labelStyle);

            if (_pinPosition.HasValue)
            {
                var coordStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 11,
                    normal = { textColor = Color.white }
                };
                Vector3 p = _pinPosition.Value;
                GUI.Label(new Rect(10, 35, 300, 20),
                    $"Pin: ({p.x:F2}, {p.y:F2}, {p.z:F2})", coordStyle);
            }
            Handles.EndGUI();
        }

        // ── ピン描画 ──
        private static void DrawPin(Vector3 position)
        {
            Vector3 top = position + Vector3.up * ShaftHeight;

            // ベース円盤
            Handles.color = PinBaseColor;
            Handles.DrawSolidDisc(position, Vector3.up, BaseDiskRadius);

            // 軸線
            Handles.color = PinShaftColor;
            Handles.DrawLine(position, top, 2f);

            // ヘッド球
            Handles.color = PinHeadColor;
            Handles.SphereHandleCap(0, top, Quaternion.identity,
                HeadRadius * 2f, EventType.Repaint);
        }

        // ── ×ボタン ──
        private static void DrawCloseButton(Vector3 pinPosition)
        {
            Vector3 buttonPos = pinPosition
                + Vector3.up * (ShaftHeight + CloseButtonOffsetY)
                + Vector3.right * CloseButtonOffsetX;

            Handles.color = CloseButtonColor;

            float size = HandleUtility.GetHandleSize(buttonPos) * CloseButtonRadius;

            if (Handles.Button(buttonPos, Quaternion.identity, size, size * 1.2f,
                    Handles.SphereHandleCap))
            {
                _pinPosition = null;
                SetActive(false);
            }

            // × テキスト
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            Handles.Label(buttonPos + Vector3.up * 0.01f, "\u00d7", style);
        }

        // ====================================================================
        // ヒエラルキー ボタン
        // ====================================================================
        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            if (!IsActive || !_pinPosition.HasValue) return;

            GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (go == null) return;

            // 選択中のオブジェクトのみボタンを表示
            var selectedObjects = Selection.gameObjects;
            if (selectedObjects == null || selectedObjects.Length == 0) return;
            if (!selectedObjects.Contains(go)) return;

            float buttonWidth = 22f;
            float padding = 2f;
            Rect buttonRect = new Rect(
                selectionRect.xMax - buttonWidth - padding,
                selectionRect.y,
                buttonWidth,
                selectionRect.height
            );

            // ボタン背景色
            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = HierarchyButtonColor;

            if (GUI.Button(buttonRect, "\u25b6"))
            {
                MoveSelectedObjectsToPin(selectedObjects, _pinPosition.Value);
            }

            GUI.backgroundColor = prevBg;
        }

        /// <summary>
        /// 選択中のオブジェクトをピン位置に移動する。
        /// 親子が同時に選択されている場合、子はフィルタして親のみ移動（子のローカル座標を維持）。
        /// </summary>
        private static void MoveSelectedObjectsToPin(GameObject[] selectedObjects, Vector3 pinPos)
        {
            // 親子重複フィルタ: 祖先が選択に含まれるオブジェクトを除外
            var selectedSet = new HashSet<GameObject>(selectedObjects);
            var rootTargets = new List<GameObject>();

            foreach (var obj in selectedObjects)
            {
                bool hasSelectedAncestor = false;
                Transform parent = obj.transform.parent;
                while (parent != null)
                {
                    if (selectedSet.Contains(parent.gameObject))
                    {
                        hasSelectedAncestor = true;
                        break;
                    }
                    parent = parent.parent;
                }
                if (!hasSelectedAncestor)
                {
                    rootTargets.Add(obj);
                }
            }

            if (rootTargets.Count == 0) return;

            Undo.SetCurrentGroupName("Pin Placement Move");
            int undoGroup = Undo.GetCurrentGroup();

            foreach (var obj in rootTargets)
            {
                Undo.RecordObject(obj.transform, "Pin Placement Move");
                obj.transform.position = pinPos;
                EditorUtility.SetDirty(obj.transform);
            }

            Undo.CollapseUndoOperations(undoGroup);

            // インスペクターに表示 + ヒエラルキーでハイライト
            EditorGUIUtility.PingObject(rootTargets[0]);
        }
    }

    // ========================================================================
    // SceneView Overlay — PMode フローティングパネル
    // ========================================================================
    [Overlay(typeof(SceneView), "pin-placement-overlay", "PMode")]
    public class ScenePinPlacementOverlay : Overlay
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
            _toggle.SetValueWithoutNotify(ScenePinPlacementTool.IsActive);
            _toggle.RegisterValueChangedCallback(evt =>
            {
                ScenePinPlacementTool.SetActive(evt.newValue);
            });
            _toggle.style.marginRight = 6;
            root.Add(_toggle);

            var label = new Label(ScenePinPlacementTool.IsActive
                ? "Active"
                : "Inactive");
            label.style.unityFontStyleAndWeight = FontStyle.Normal;
            root.Add(label);

            ScenePinPlacementTool.OnActiveChanged += () =>
            {
                _toggle?.SetValueWithoutNotify(ScenePinPlacementTool.IsActive);
                label.text = ScenePinPlacementTool.IsActive
                    ? "Active"
                    : "Inactive";
            };

            return root;
        }
    }
}

