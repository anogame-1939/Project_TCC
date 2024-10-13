using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace NovelEditor.Editor
{
    /// <summary>
    /// NovelEditor用に使用するGraphViewのクラス
    /// </summary>
    internal class NovelGraphView : GraphView
    {
        public System.Action<DropdownMenuAction> OnContextMenuNodeCreate;
        public SerializeGraphElementsDelegate CopyNodes;
        public System.Action<string, BaseNode> PasteOnNode;
        public System.Action<string, Vector2> PasteOnGraph;

        public NovelGraphView()
        {
            CreateGraph();

            SettingGraph();
        }

        /// <summary>
        /// GraphViewの見た目の設定
        /// </summary>
        void CreateGraph()
        {
            // 親のサイズに合わせてGraphViewのサイズを設定
            this.StretchToParentSize();
            //背景をグリッドに
            styleSheets.Add(Resources.Load<StyleSheet>("EditorBackGround"));
            GridBackground gridBackground = new GridBackground();
            Insert(0, gridBackground);
        }

        /// <summary>
        /// GraphViewの初期設定
        /// </summary>
        void SettingGraph()
        {
            //ズームの設定
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            // スクロールでズームインアウトができるように
            if (NovelEditorWindow.editingData != null)
            {
                this.UpdateViewTransform(NovelEditorWindow.editingData.graphPosition, NovelEditorWindow.editingData.graphScale);
            }
            viewTransformChanged = viewChanged;
            // ドラッグで描画範囲を動かせるように
            this.AddManipulator(new ContentDragger());
            // ドラッグで選択した要素を動かせるように
            this.AddManipulator(new SelectionDragger());
            // ドラッグで範囲選択ができるように
            this.AddManipulator(new RectangleSelector());
        }

        /// <summary>
        /// グラフ変化時に画面位置やズームの数値を保存する
        /// </summary>
        /// <param name="graphView">変化したGraphView</param>
        void viewChanged(GraphView graphView)
        {
            if (NovelEditorWindow.editingData != null)
            {
                NovelEditorWindow.editingData.graphScale = graphView.viewTransform.scale;
                NovelEditorWindow.editingData.graphPosition = graphView.viewTransform.position;
                EditorUtility.SetDirty(NovelEditorWindow.editingData);
            }
        }

        //ノードのルールの設定
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();

            compatiblePorts.AddRange(ports.ToList().Where(port =>
            {
                // 同じノードには繋げない
                if (startPort.node == port.node)
                    return false;

                // Input同士、Output同士は繋げない
                if (port.direction == startPort.direction)
                    return false;

                // ポートの型が一致していない場合は繋げない
                if (port.portType != startPort.portType)
                    return false;

                return true;
            }));

            return compatiblePorts;
        }

        //右クリックで表示されるメニュー
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            // Create Nodeメニュー
            if (evt.target is GraphView && nodeCreationRequest != null)
            {
                evt.menu.AppendAction(
                    "Create Node",
                    OnContextMenuNodeCreate,
                    DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendSeparator();
            }
            // Copyメニュー
            if ((evt.target is BaseNode))
            {
                evt.menu.AppendAction(
                    "Copy",
                    copy =>
                    {
                        serializeGraphElements = CopyNodes;
                        CopySelectionCallback();
                    },
                    copy => (this.canCopySelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled),
                    (object)null);
            }
            // Pasteしてノードを新規作成
            if (evt.target is GraphView)
            {
                Vector2 position = evt.mousePosition;
                evt.menu.AppendAction(
                    "Paste",
                    paste =>
                    {
                        unserializeAndPaste = new UnserializeAndPasteDelegate(
                            (string operationName, string pasteData) => PasteOnGraph(pasteData, position)
                        );
                        PasteCallback();
                    },
                    paste => (this.canPaste ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled),
                    (object)null);
            }

            //Pasteしてノードを上書き
            if (evt.target is BaseNode)
            {
                BaseNode node = (BaseNode)evt.target;
                evt.menu.AppendAction(
                    "Paste",
                    Paste =>
                    {
                        unserializeAndPaste = new UnserializeAndPasteDelegate(
                            (string operationName, string pasteData) => PasteOnNode(pasteData, node)
                        );
                        PasteCallback();
                    },
                    Paste => (this.canPaste ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled),
                    (object)null);
            }

            // Delete
            if (evt.target is GraphView || evt.target is BaseNode || evt.target is Group || evt.target is Edge)
            {
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Delete", (a) => { DeleteSelectionCallback(AskUser.DontAskUser); },
                    (a) => { return canDeleteSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled; });
            }
        }

    }
}