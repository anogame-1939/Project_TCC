using UnityEditor;
using UnityEngine;

namespace AnoGame.Editor.Tools
{
    /// <summary>
    /// シーン上にピンを立て、ヒエラルキーのボタンからオブジェクトをピン位置に移動するツール。
    /// </summary>
    [InitializeOnLoad]
    public static class ScenePinPlacementTool
    {
        // ── 状態 ──
        private static bool _isActive;
        private static Vector3? _pinPosition;

        // ── 描画設定 ──
        private static readonly Color PinBaseColor = new Color(1f, 0.85f, 0f, 0.6f);   // 半透明イエロー
        private static readonly Color PinShaftColor = new Color(0.9f, 0.2f, 0.2f, 1f);  // 赤
        private static readonly Color PinHeadColor = new Color(0.9f, 0.15f, 0.15f, 1f); // 赤
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
            _isActive = !_isActive;
            if (!_isActive)
            {
                _pinPosition = null;
            }
            SceneView.RepaintAll();
            EditorApplication.RepaintHierarchyWindow();
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleModeValidate()
        {
            Menu.SetChecked(MenuPath, _isActive);
            return true;
        }

        // ====================================================================
        // SceneView 描画 + クリック検知
        // ====================================================================
        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!_isActive) return;

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            // ── クリック検知 ──
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

                // Raycast でシーン上の位置を取得
                Vector3 hitPoint;
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    hitPoint = hit.point;
                }
                else
                {
                    // コライダーが無い場合は XZ 平面 (Y=0) との交差
                    Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                    if (groundPlane.Raycast(ray, out float distance))
                    {
                        hitPoint = ray.GetPoint(distance);
                    }
                    else
                    {
                        return; // 交差なし
                    }
                }

                // Y=0 固定
                _pinPosition = new Vector3(hitPoint.x, 0f, hitPoint.z);

                e.Use();
                SceneView.RepaintAll();
                EditorApplication.RepaintHierarchyWindow();
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
                _isActive = false;
                SceneView.RepaintAll();
                EditorApplication.RepaintHierarchyWindow();
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
            if (!_isActive || !_pinPosition.HasValue) return;

            GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (go == null) return;

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
                Undo.RecordObject(go.transform, "Pin Placement Move");
                go.transform.position = _pinPosition.Value;
                EditorUtility.SetDirty(go.transform);
            }

            GUI.backgroundColor = prevBg;
        }
    }
}
