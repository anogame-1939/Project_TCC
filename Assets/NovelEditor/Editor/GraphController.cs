using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using NovelEditor;

namespace NovelEditor.Editor
{
    /// <summary>
    /// グラフの管理を行うクラス
    /// </summary>
    internal class GraphController
    {
        NovelGraphView graphView;

        /// <summary>
        /// グラフの作成
        /// </summary>
        /// <returns>作成したグラフ</returns>
        internal NovelGraphView CreateGraph()
        {
            graphView = new NovelGraphView();

            if (NovelEditorWindow.editingData != null)
            {
                //グラフが変化した時の処理
                graphView.graphViewChanged += OnGraphChange;

                //右クリックで表示できるメニューの作成
                SetMenu();

                LoadNodes();

                Undo.undoRedoPerformed  += UndoRedoGraph;

            }

            return graphView;
        }

        /// <summary>
        /// UndoRedoをした時の処理
        /// </summary>
        void UndoRedoGraph(){
            NovelData.NodeData data = BaseNode.nowSelection?.nodeData;

            //グラフを作り直す
            foreach (var element in graphView.graphElements)
            {
                if (element is BaseNode || element is Edge)
                {
                    element.RemoveFromHierarchy();
                }
            }
            LoadNodes();

            //UndoRedoをする前に選択していたデータを表示
            if (data != null)
            {
                Selection.activeObject = null;
                if (data is NovelData.ParagraphData && ParagraphNode.nodes.Count > data.index)
                {
                    ParagraphNode.nodes[data.index]?.OnSelected();
                }
                else if (ChoiceNode.nodes.Count > data.index)
                {
                    ChoiceNode.nodes[data.index]?.OnSelected();
                }
            }
        }
        
        /// <summary>
        /// 右クリックで表示するメニューを作成する
        /// </summary>
        void SetMenu(){
            MenuWindow menuWindow = ScriptableObject.CreateInstance<MenuWindow>();
            menuWindow.Init(graphView,Resources.FindObjectsOfTypeAll<NovelEditorWindow>()[0] as EditorWindow);

            graphView.nodeCreationRequest += menuWindow.nodeCreationRequest;
            graphView.OnContextMenuNodeCreate = menuWindow.OnContextMenuNodeCreate;
            graphView.CopyNodes = menuWindow.OnContextMenuNodeCopy;
            graphView.PasteOnNode = menuWindow.OnContextMenuPasteOnNode;
            graphView.PasteOnGraph = menuWindow.OnContextMenuPasteOnGraph;
        }

        /// <summary>
        /// データからノードを作成する
        /// </summary>
        internal void LoadNodes()
        {
            //データからノードを作る
            NodeCreator.RestoreGraph(graphView, NovelEditorWindow.editingData);
        }

        //グラフが変化した時の処理
        public GraphViewChange OnGraphChange(GraphViewChange change)
        {
            //エッジが作成されたとき、接続情報を保存
            if (change.edgesToCreate != null)
            {
                Undo.RecordObject(NovelEditorWindow.editingData, "Create Edge");
                //作成された全てのエッジを取得
                foreach (Edge edge in change.edgesToCreate)
                {
                    //ノード同士の接続
                    if (edge.output.node is BaseNode && edge.input.node is BaseNode)
                    {
                        ((BaseNode)edge.output.node).AddNext((BaseNode)edge.input.node, edge.output);
                    }
                }

            }

            //何かが削除された時
            if (change.elementsToRemove != null)
            {
                Undo.RecordObject(NovelEditorWindow.editingData, "Delete Graph Elememts");
                //全ての削除された要素を取得
                foreach (GraphElement e in change.elementsToRemove)
                {
                    //ノードが削除されたとき
                    if (e is BaseNode)
                    {
                        ((BaseNode)e).DeleteNode();
                    }

                    //エッジが削除されたとき
                    if (e.GetType() == typeof(Edge))
                    {
                        Edge edge = (Edge)e;
                        ((BaseNode)edge.output.node).ResetNext(edge);
                    }
                }

            }

            //ノードが動いた時、位置を保存
            if (change.movedElements != null)
            {
                foreach (GraphElement e in change.movedElements)
                {
                    if (e is BaseNode)
                    {
                        ((BaseNode)e).SaveCurrentPosition();
                    }
                }
            }

            if (NovelEditorWindow.editingData != null)
                EditorUtility.SetDirty(NovelEditorWindow.editingData);

            return change;
        }
    }
}