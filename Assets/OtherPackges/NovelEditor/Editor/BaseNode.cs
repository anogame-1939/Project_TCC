using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using NovelEditor;

namespace NovelEditor.Editor
{
    /// <summary>
    /// 使用するノードの基底クラス
    /// </summary>
    internal abstract class BaseNode : Node
    {
        /// <summary>
        /// 各ノードが所有しているデータ
        /// </summary>
        public NovelData.NodeData nodeData;

        /// <summary>
        /// 使用する入力ポート
        /// </summary>
        internal Port InputPort { get; private protected set; }

        /// <summary>
        /// 使用する出力用ポート(一つだけ)
        /// </summary>
        internal Port ContinuePort { get; private protected set; }

        /// <summary>
        /// 現在選択しているノード
        /// </summary>
        public static BaseNode nowSelection { get; private set; }

        /// <summary>
        /// ノードのタイトルを設定する
        /// </summary>
        protected abstract void SetTitle();

        /// <summary>
        /// ノードの持つデータを上書きする。
        /// </summary>
        /// <param name="pasteData">コピーしたノードの持つデータのJsonデータ</param>
        internal abstract void OverwriteNode(string pasteData);

        /// <summary>
        /// 次のノードの情報を設定する
        /// </summary>
        /// <param name="nextNode">新しく接続されたノード</param>
        /// <param name="outPort">接続したポート</param>
        internal abstract void AddNext(BaseNode nextNode, Port outPort);

        /// <summary>
        /// 次に接続されたノードの情報を削除する
        /// </summary>
        /// <param name="edge">削除されたエッジ</param>
        public abstract void ResetNext(Edge edge);

        /// <summary>
        /// ノードの初期化を行う
        /// </summary>
        private protected virtual void NodeSet()
        {
            titleButtonContainer.Clear(); // デフォルトのCollapseボタンを削除

            RegisterCallback<MouseDownEvent>(MouseDowned);
        }

        /// <summary>
        /// ノードを選択した時のインスペクター表示
        /// </summary>
        public override void OnSelected()
        {
            if (nodeData != null)
            {
                nowSelection = this;
            }
            else
            {
                Selection.activeObject = null;
                nowSelection = null;
            }

        }

        /// <summary>
        /// ノードの選択を解除した時にインスペクター表示を消す
        /// </summary>
        public override void OnUnselected()
        {
            Selection.activeObject = null;
            nowSelection = null;
            SetTitle();
        }

        /// <summary>
        /// ノードの位置を設定する
        /// </summary>
        /// <param name="rect">ノードの位置、大きさ</param>
        public override void SetPosition(Rect rect)
        {
            base.SetPosition(rect);
            SavePosition(rect);
        }

        /// <summary>
        /// 現在のノードの位置をデータに保存する
        /// </summary>
        public void SaveCurrentPosition()
        {
            SavePosition(GetPosition());
        }

        /// <summary>
        /// ノードを消す
        /// </summary>
        public void DeleteNode()
        {
            nodeData.SetNodeDeleted(NovelEditorWindow.editingData);
        }

        /// <summary>
        /// クリックされた時
        /// </summary>
        private protected void MouseDowned(MouseEventBase<MouseDownEvent> evt)
        {
            OnSelected();
        }

        /// <summary>
        /// ノードの位置をデータに保存する
        /// </summary>
        protected void SavePosition(Rect rect)
        {
            nodeData.SavePosition(rect);
        }
    }
}