using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace NovelEditor
{
    /// <summary>
    /// NovelEditorで使用されるデータのScriptableObject
    /// </summary>
    [CreateAssetMenu(menuName = "NovelData")]
    public class NovelData : ScriptableObject
    {
        #region 会話基本データ
        //立ち絵の位置
        [SerializeField] List<Image> _locations = new List<Image>();

        //段落のノードのリスト
        [SerializeField, HideInInspector]
        List<ParagraphData> _paragraphList = new List<ParagraphData>();

        //選択肢のノードのリスト
        [SerializeField, HideInInspector]
        List<ChoiceData> _choiceList = new List<ChoiceData>();

        #endregion

        #region グラフ情報
        //グラフのズーム量
        [SerializeField, HideInInspector] internal Vector3 graphScale = new Vector3(1, 1, 1);
        [SerializeField, HideInInspector] internal Vector3 graphPosition = new Vector3(0, 0, 0);
        #endregion

        #region エディタから使用する情報
        //段落のノード数(非アクティブ含む)
        internal int MaxParagraphID => _paragraphList.Count;
        //選択肢のノード数(非アクティブ含む)
        internal int MaxChoiceCnt => _choiceList.Count;
        //データが新しいか
        internal bool newData => _paragraphList.Count == 0;

        [SerializeField, HideInInspector]
        internal bool havePreLocations = false;

        [SerializeField] internal List<Image> newLocations = new List<Image>();

        //使っていないデータを入れるスタック
        [SerializeField, HideInInspector]
        internal Stack<ParagraphData> ParagraphStack = new Stack<ParagraphData>();

        //使ってないデータを入れるスタック
        [SerializeField, HideInInspector]
        internal Stack<ChoiceData> ChoiceStack = new Stack<ChoiceData>();


        #endregion

        #region プロパティ
        /// <summary>
        /// 立ち絵の位置のリスト
        /// </summary>
        public List<Image> locations => _locations;
        /// <summary>
        /// データに含まれる会話ノードのデータのリスト
        /// 未使用のデータも含まれます。グラフ内にあるかはParagraphDataのisActiveで判断できます
        /// </summary>
        public List<ParagraphData> paragraphList => _paragraphList;
        /// <summary>
        /// データに含まれる選択肢のデータのリスト
        /// 未使用のデータも含まれます。グラフ内にあるかはChoiceDataのisActiveで判断できます
        /// </summary>
        public List<ChoiceData> choiceList => _choiceList;

        #endregion

        internal void changeLocation(List<Image> newLocations)
        {
            _locations = newLocations;
        }

        internal void ResetData()
        {
            _paragraphList.Clear();

            ParagraphData pdata = CreateParagraph();
            pdata.dialogueList[0].text = "FirstParagraph";
            pdata.SetIndex(0);
        }

        internal ParagraphData CreateParagraph()
        {
            ParagraphData data;
            if (ParagraphStack.Count == 0)
            {
                data = new ParagraphData();
                data.SetIndex(MaxParagraphID);
                _paragraphList.Add(data);
            }
            else
            {
                data = ParagraphStack.Pop();
            }

            data.SetEnable(true);
            data.dialogueList.Add(new ParagraphData.Dialogue());
            data.dialogueList[0].text = "Paragraph";
            data.dialogueList[0].charas = new Sprite[locations.Count];
            data.dialogueList[0].howCharas = new CharaChangeStyle[locations.Count];
            data.dialogueList[0].charaFadeColor = new Color[locations.Count];
            data.dialogueList[0].charaEffects = new Effect[locations.Count];
            data.dialogueList[0].charaEffectStrength = new int[locations.Count];
            data.ResetNext(Next.End);

            return data;
        }

        internal ParagraphData CreateParagraphFromJson(string sdata)
        {
            ParagraphData data = JsonUtility.FromJson<ParagraphData>(sdata);
            ParagraphData popData = CreateParagraph();
            popData.ChangeDialogue(data.dialogueList);
            return popData;
        }

        internal ChoiceData CreateChoice()
        {
            ChoiceData data;
            if (ChoiceStack.Count == 0)
            {
                data = new ChoiceData();
                data.SetIndex(MaxChoiceCnt);
                _choiceList.Add(data);
            }
            else
            {
                data = ChoiceStack.Pop();
            }
            data.text = "Choice";
            data.SetEnable(true);

            return data;
        }

        internal ChoiceData CreateChoiceFromJson(string sdata)
        {
            ChoiceData data = JsonUtility.FromJson<ChoiceData>(sdata);
            ChoiceData popData = CreateChoice();
            popData.text = data.text;
            return popData;
        }


        /// <summary>
        /// ノードの基本データです
        /// </summary>
        [System.SerializableAttribute]
        public abstract class NodeData
        {
            #region ノード基本データ
            [SerializeField, HideInInspector] protected bool _enabled = true;
            [SerializeField, HideInInspector] int _index;
            [SerializeField, HideInInspector] Rect _nodePosition;
            [SerializeField, HideInInspector] int _nextParagraphIndex = -1;
            public string nodeName;
            #endregion

            #region プロパティ
            internal bool enabled => _enabled;
            /// <summary>
            /// 現在グラフ内で使用されているノードかどうか
            /// </summary>
            public bool isActive => _enabled;
            /// <summary>
            /// NovelDataのリストでのIndex
            /// </summary>
            public int index => _index;
            /// <summary>
            /// 次の会話ノードのNovelDataのリストでのIndex
            /// </summary>
            public int nextParagraphIndex => _nextParagraphIndex;
            internal Rect nodePosition => _nodePosition;

            #endregion

            internal void SavePosition(Rect rect)
            {
                _nodePosition = rect;
            }
            internal abstract void SetNodeDeleted(NovelData editingData);
            internal void ChangeNextParagraph(int nextIndex)
            {
                _nextParagraphIndex = nextIndex;
            }
            internal void SetIndex(int newIndex)
            {
                _index = newIndex;
            }

            internal void SetEnable(bool flag)
            {
                _enabled = flag;
            }

        }

        /// <summary>
        /// 選択肢のデータです
        /// </summary>
        [System.SerializableAttribute]
        public class ChoiceData : NodeData
        {
            /// <summary>
            /// 選択肢のテキスト
            /// </summary>
            public string text;

            internal override void SetNodeDeleted(NovelData editingData)
            {
                this.SetEnable(false);
                editingData.ChoiceStack.Push(this);
            }
        }

        /// <summary>
        /// 会話ノードのデータです
        /// </summary>
        [System.SerializableAttribute]
        public class ParagraphData : NodeData
        {
            #region 段落の基本データ
            [SerializeField, HideInInspector]
            List<Dialogue> _dialogueList = new List<Dialogue>();

            [SerializeField, HideInInspector]
            internal bool detailOpen = false;

            [SerializeField, HideInInspector]
            Next _next = Next.End;

            //次のポート番号,choiceID
            [SerializeField, HideInInspector]
            List<int> _nextChoiceIndexes = new List<int>();

            #endregion

            #region プロパティ
            /// <summary>
            /// セリフのリスト
            /// </summary>
            public List<Dialogue> dialogueList => _dialogueList;
            /// <summary>
            /// 次のノードの種類
            /// </summary>
            public Next next => _next;
            /// <summary>
            /// 接続されている選択肢のリスト
            /// </summary>
            public List<int> nextChoiceIndexes => _nextChoiceIndexes;
            #endregion

            internal void RemoveChoice(int removeIndex)
            {
                _nextChoiceIndexes.RemoveAt(removeIndex);
            }
            internal void ChangeNextChoice(int portIndex, int nextIndex)
            {
                _nextChoiceIndexes[portIndex] = nextIndex;
            }
            internal void ResetNext(Next newNext)
            {
                _next = newNext;
                _nextChoiceIndexes.Clear();
                _nextChoiceIndexes.Add(-1);
                ChangeNextParagraph(-1);
            }
            internal void AddNext()
            {
                _nextChoiceIndexes.Add(-1);
            }

            internal override void SetNodeDeleted(NovelData editingData)
            {
                this.ChangeNextParagraph(-1);
                this._dialogueList.Clear();
                this.SetEnable(false);
                editingData.ParagraphStack.Push(this);
            }


            internal void ChangeDialogue(List<Dialogue> newDialogueList)
            {
                _dialogueList = newDialogueList;
            }

            /// <summary>
            /// 現在の背景、立ち絵の情報を更新するための関数
            /// </summary>
            /// <param name="editingData">このデータが属するNovelData</param>
            internal void UpdateOrder(NovelData editingData)
            {
                int back = 0;
                int[] charas = new int[editingData.locations.Count];


                foreach (var data in dialogueList)
                {
                    for (int i = 0; i < editingData.locations.Count; i++)
                    {
                        if (data.howCharas[i] == CharaChangeStyle.UnChange)
                        {
                            if (charas[i] == 0)
                            {
                                data.charas[i] = null;
                            }
                            else
                            {
#if UNITY_EDITOR
                                data.charas[i] = (Sprite)EditorUtility.InstanceIDToObject(charas[i]);
#endif
                            }

                        }
                        else
                        {
                            if (data.charas[i] == null)
                            {
                                charas[i] = 0;
                            }
                            else
                            {
                                charas[i] = data.charas[i].GetInstanceID();
                            }

                        }
                    }

                    if (data.howBack == BackChangeStyle.UnChange)
                    {
                        if (back == 0)
                        {
                            data.back = null;
                        }
                        else
                        {
#if UNITY_EDITOR
                            data.back = (Sprite)EditorUtility.InstanceIDToObject(back);
#endif
                        }
                    }
                    else
                    {
                        if (data.back == null)
                        {
                            back = 0;
                        }
                        else
                        {
                            back = data.back.GetInstanceID();
                        }
                    }
                }

            }

            /// <summary>
            /// セリフごとのデータ
            /// </summary>
            [System.SerializableAttribute]
            public class Dialogue
            {
                internal int index = 0;
                public bool open;
                public string Name = "";
                public string text;

                public Sprite back;
                public BackChangeStyle howBack;
                public Color backFadeColor = Color.white;
                public float backFadeSpeed = 1;

                [SerializeField] public CharaChangeStyle[] howCharas;
                [SerializeField] public Sprite[] charas;
                [SerializeField] public Color[] charaFadeColor;

                public bool changeFont = false;
                public TMP_FontAsset font;
                public Color fontColor = Color.white;
                public int fontSize = 30;

                public bool changeNameFont = false;
                public TMP_FontAsset nameFont;
                public Color nameColor = Color.white;


                public AudioClip BGM;
                public SoundStyle BGMStyle;
                public LoopMode BGMLoop;
                public int BGMCount = 1;
                public float BGMSecond = 20;
                public float BGMFadeTime = 3;
                public float BGMEndFadeTime = 3;

                public AudioClip SE;
                public SoundStyle SEStyle;
                public LoopMode SELoop;
                public int SECount = 1;
                public float SESecond = 20;
                public float SEFadeTime = 3;
                public float SEEndFadeTime = 3;


                public Effect backEffect;
                public int backEffectStrength;
                public Effect[] charaEffects;
                public int[] charaEffectStrength;
                public Effect DialogueEffect;
                public int DialogueEffectStrength;
            }

        }

    }

}
