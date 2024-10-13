using UnityEngine;
using UnityEditor;
using NovelEditor;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace NovelEditor.Editor
{
    /// <summary>
    /// グラフを復元するためのクラス
    /// </summary>
    internal static class NodeCreator
    {
        /// <summary>
        /// データからグラフを復元
        /// </summary>
        /// <param name="graphView">ノードを貼り付けるGraphView</param>
        /// <param name="data">復元するデータ</param>
        internal static void RestoreGraph(GraphView graphView, NovelData data)
        {
            RestoreParagraphNode(graphView, data.paragraphList);
            RestoreChoiceNode(graphView, data.choiceList);
            RestoreParagraphEdge(graphView);
            RestoreChoiceEdge(graphView);
        }

        static void RestoreParagraphNode(GraphView graphView, List<NovelData.ParagraphData> paragraphData)
        {
            ParagraphNode.nodes.Clear();
            for (int i = 0; i < paragraphData.Count; i++)
            {
                ParagraphNode.nodes.Add(null);
            }

            foreach (NovelData.ParagraphData pdata in paragraphData)
            {
                if (pdata.enabled)
                {
                    ParagraphNode node = new ParagraphNode(pdata);
                    graphView.AddElement(node);
                }
            }
        }

        static void RestoreChoiceNode(GraphView graphView, List<NovelData.ChoiceData> choiceData)
        {
            ChoiceNode.nodes.Clear();
            for (int i = 0; i < choiceData.Count; i++)
            {
                ChoiceNode.nodes.Add(null);
            }

            foreach (NovelData.ChoiceData cdata in choiceData)
            {
                if (cdata.enabled)
                {
                    ChoiceNode node = new ChoiceNode(cdata);
                    graphView.AddElement(node);
                }
            }
        }

        /// <summary>
        /// ParagraphNodeから繋がれるエッジを復元する
        /// </summary>
        /// <param name="graphView">エッジをつけるGraphView</param>
        static void RestoreParagraphEdge(GraphView graphView)
        {
            foreach (ParagraphNode node in ParagraphNode.nodes)
            {
                if (node == null) continue;

                if (node.data.next == Next.Continue)
                {
                    //ParagraphからParagraphにつなぐ
                    if (node.data.nextParagraphIndex == -1)
                        continue;

                    Edge edge = node.ContinuePort.ConnectTo(ParagraphNode.nodes[node.data.nextParagraphIndex].InputPort);
                    graphView.AddElement(edge);
                }

                else if (node.data.next == Next.Choice)
                {
                    //ParagraphからChoiceにつなぐ
                    for (int i = 0; i < node.data.nextChoiceIndexes.Count; i++)
                    {
                        int index = node.data.nextChoiceIndexes[i];
                        if (index == -1)
                            continue;

                        Edge edge = node.choicePorts[i].ConnectTo(ChoiceNode.nodes[index].InputPort);
                        graphView.AddElement(edge);
                    }
                }
            }
        }

        /// <summary>
        /// ChoiceNodeから繋がれるエッジを復元する
        /// </summary>
        /// <param name="graphView">エッジをつけるGraphView</param>
        static void RestoreChoiceEdge(GraphView graphView)
        {
            //ノードを接続する
            foreach (ChoiceNode node in ChoiceNode.nodes)
            {
                if (node == null) continue;
                //ChoiceからParagraphにつなぐ
                if (node.data.nextParagraphIndex == -1)
                    continue;

                Edge edge = node.ContinuePort.ConnectTo(ParagraphNode.nodes[node.data.nextParagraphIndex].InputPort);
                graphView.AddElement(edge);
            }
        }


    }
}