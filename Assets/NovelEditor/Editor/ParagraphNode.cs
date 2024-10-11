using UnityEngine;
using UnityEditor;
using NovelEditor;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using UnityEditor.UIElements;
using System;
using System.Linq;
using Newtonsoft.Json.Linq;


namespace NovelEditor.Editor
{
    /// <summary>
    /// 会話用のノードのクラス
    /// </summary>
    internal class ParagraphNode : BaseNode
    {
        /// <summary>
        /// ParagraphNodeのリスト、使用されていないノードも含む
        /// </summary>
        public static List<ParagraphNode> nodes = new List<ParagraphNode>();
        
        /// <summary>
        /// ノードの持つ会話のデータ
        /// </summary>
        public NovelData.ParagraphData data => (NovelData.ParagraphData)nodeData;
        
        /// <summary>
        /// 選択肢のポート
        /// </summary>
        /// <typeparam name="Port"></typeparam>
        /// <returns></returns>
        internal List<Port> choicePorts = new List<Port>();


        /// <summary>
        /// ノードを作成するコンストラクタ。データを新しく作成する
        /// </summary>
        public ParagraphNode()
        {
            //データを作成する
            nodeData = NovelEditorWindow.editingData.CreateParagraph();
            NodeSet();
            nodes.Add(this);
        }


        /// <summary>
        /// 指定されたデータでノードを作成するコンストラクタ
        /// </summary>
        /// <param name="Pdata">ノードに設定するデータ</param>
        public ParagraphNode(NovelData.ParagraphData Pdata)
        {
            nodeData = Pdata;

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

        internal override void OverwriteNode(string pasteData)
        {
            NovelData.ParagraphData newData = JsonUtility.FromJson<NovelData.ParagraphData>(pasteData);
            data.ChangeDialogue(newData.dialogueList);
            SetTitle();
            OnSelected();
        }

        private protected override void NodeSet()
        {
            base.NodeSet();
            //ノードの色変更
            if (data.index == 0)
            {
                capabilities -= Capabilities.Deletable;
                titleContainer.style.backgroundColor = new Color(0.8f, 0.2f, 0.4f);
            }
            else
            {
                titleContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.4f);
            }

            SetTitle();

            //ボタン作成
            styleSheets.Add(Resources.Load<StyleSheet>("PNodeUSS"));
            var visualTree = Resources.Load<VisualTreeAsset>("NodeUXML");
            visualTree.CloneTree(titleButtonContainer);
            var addbutton = titleButtonContainer.Q<Button>("addChoiceButton");
            addbutton.clickable.clicked += () =>
            {
                data.AddNext();
                AddChoicePort();
            };

            //ドロップダウン作成
            var nextStateField = new EnumField(data.next);
            nextStateField.RegisterValueChangedCallback(evt =>
            { FieldChangedEvent((Next)nextStateField.value); });
            mainContainer.Add(nextStateField);

            //InputPort作成
            InputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(BaseNode));
            InputPort.portName = "prev";
            inputContainer.Add(InputPort);

            //OutputPort作成
            InitOutputPort();
        }

        protected override void SetTitle()
        {
            title = "Paragraph";
            if (data != null && data.dialogueList.Count > 0)
            {
                title = data.dialogueList[0].text.Substring(0, Math.Min(data.dialogueList[0].text.Length, 10));
            }
        }

        protected void AddChoicePort()
        {
            if (data.next == Next.Choice)
            {
                //-ボタン作成
                Button removePortButton = new Button();
                removePortButton.text = "-";
                //ChoicePort作成
                Port outputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(ChoiceNode));
                outputPort.portColor = new Color(0.7f, 0.7f, 0.0f);
                removePortButton.clickable.clicked += () =>
                {
                    RemoveChoicePort(removePortButton, outputPort);
                };
                outputPort.portName = "choice" + (choicePorts.Count + 1).ToString();
                outputContainer.Add(outputPort);
                outputContainer.Add(removePortButton);
                choicePorts.Add(outputPort);

                RefreshPorts();
                RefreshExpandedState();
            }
        }

        private void RemoveChoicePort(Button rmvButton, Port outPort)
        {

            int index = choicePorts.IndexOf(outPort);
            choicePorts.RemoveAt(index);
            data.RemoveChoice(index);

            //ポートから生えてるエッジを削除する
            foreach (Edge e in outPort.connections)
            {
                e.input.Disconnect(e);
                e.RemoveFromHierarchy();
            }

            //Choiceポートとボタンを削除する
            outputContainer.Remove(rmvButton);
            outputContainer.Remove(outPort);

        }

        private void InitOutputPort()
        {
            outputContainer.Clear();
            switch (data.next)
            {
                case Next.Continue:
                    ContinuePort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(BaseNode));
                    ContinuePort.portColor = new Color(0.8f, 0.2f, 0.4f);
                    ContinuePort.portName = "next";
                    outputContainer.Add(ContinuePort);
                    break;

                case Next.Choice:
                    //必要な数分だけ作る
                    for (int i = 0; i < data.nextChoiceIndexes.Count; i++)
                    {
                        AddChoicePort();
                    }

                    break;
            }
            RefreshExpandedState();
        }

        private void FieldChangedEvent(Next nextValue)
        {
            data.ResetNext(nextValue);
            RemoveAllOutEdge();
            InitOutputPort();
        }

        private void RemoveAllOutEdge()
        {
            foreach (Port i in choicePorts)
            {
                foreach (Edge e in i.connections)
                {
                    e.input.Disconnect(e);
                    e.RemoveFromHierarchy();
                }
            }
            choicePorts = new List<Port>();

            if (ContinuePort != null)
            {
                foreach (Edge e in ContinuePort.connections)
                {
                    e.input.Disconnect(e);
                    e.RemoveFromHierarchy();
                }
            }
        }

        internal override void AddNext(BaseNode nextNode, Port outPort)
        {
            if (nextNode is ParagraphNode)
            {
                data.ChangeNextParagraph(((ParagraphNode)nextNode).data.index);
            }
            if (nextNode is ChoiceNode)
            {
                int portNum = choicePorts.IndexOf(outPort);
                data.ChangeNextChoice(portNum, ((ChoiceNode)nextNode).data.index);
            }
        }
        public override void ResetNext(Edge edge)
        {
            if (edge.input.node is ChoiceNode)
            {
                int index = choicePorts.IndexOf(edge.output);
                data.ChangeNextChoice(index, -1);
            }

            if (edge.input.node is ParagraphNode)
            {
                data.ChangeNextParagraph(-1);
            }

        }

        public override void OnSelected(){
            base.OnSelected();
            TempParagraph temp = ScriptableObject.CreateInstance<TempParagraph>();
            temp.data = data;
            temp.dialogueList = data.dialogueList;
            Selection.activeObject = temp;
        }
    }
}