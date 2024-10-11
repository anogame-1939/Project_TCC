using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using NovelEditor;
using NovelEditor.Editor;

namespace NovelEditor.Editor
{
    /// <summary>
    /// 選択肢のノードのクラス
    /// </summary>
    internal class ChoiceNode : BaseNode
    {
        /// <summary>
        /// ChoiceNodeのリスト、使用されていないノードも含む
        /// </summary>
        /// <typeparam name="ChoiceNode"></typeparam>
        public static List<ChoiceNode> nodes = new List<ChoiceNode>();

        /// <summary>
        /// ノードの持つ選択肢のデータ
        /// </summary>
        public NovelData.ChoiceData data => (NovelData.ChoiceData)nodeData;

        /// <summary>
        /// ノードを作成するコンストラクタ。データを新しく作成する
        /// </summary>
        public ChoiceNode()
        {
            //データを作成する
            nodeData = NovelEditorWindow.editingData.CreateChoice();
            NodeSet();
            nodes.Add(this);
        }

        /// <summary>
        /// 指定されたデータでノードを作成するコンストラクタ
        /// </summary>
        /// <param name="Cdata">ノードに設定するデータ</param>
        public ChoiceNode(NovelData.ChoiceData Cdata)
        {
            nodeData = Cdata;

            NodeSet();
            SetPosition(data.nodePosition);
            if (data.index < nodes.Count)
            {
                nodes[data.index] = this;
            }
            else
            {
                nodes.Add(this);
            }
        }

        public override void ResetNext(Edge edge)
        {
            data.ChangeNextParagraph(-1);
        }

        public override void OnSelected(){
            base.OnSelected();
            TempChoice temp = ScriptableObject.CreateInstance<TempChoice>();
            temp.data = data;
            Selection.activeObject = temp;
        }

        internal override void OverwriteNode(string pasteData)
        {
            NovelData.ChoiceData newData = JsonUtility.FromJson<NovelData.ChoiceData>(pasteData);
            data.text = newData.text;
            SetTitle();
        }

        private protected override void NodeSet()
        {
            base.NodeSet();

            SetTitle();

            //InputPort作成
            InputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(ChoiceNode));
            InputPort.portColor = new Color(0.7f, 0.7f, 0.0f);
            InputPort.portName = "prev";
            inputContainer.Add(InputPort);

            //OutputPort作成
            ContinuePort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(BaseNode));
            ContinuePort.portName = "next";
            outputContainer.Add(ContinuePort);
        }

        protected override void SetTitle()
        {
            title = "Choice";
            if (data != null)
            {
                title = data.text.Substring(0, Math.Min(data.text.Length, 10));
            }
        }

        internal override void AddNext(BaseNode nextNode, Port outPort)
        {
            data.ChangeNextParagraph(((ParagraphNode)nextNode).data.index);
        }
    }
}