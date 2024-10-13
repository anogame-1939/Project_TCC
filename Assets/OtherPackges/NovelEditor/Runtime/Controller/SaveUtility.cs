using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TMPro;

namespace NovelEditor
{
    /// <summary>
    /// データのセーブ、ロードなどを行う
    /// </summary>
    internal class SaveUtility
    {
        /// <summary>
        /// ロードの進捗を0~1で返す
        /// </summary>
        /// <value>ロードの進捗</value>
        internal float progress { get; private set; } = 0;

        static SaveUtility instance;

        internal static SaveUtility Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SaveUtility();
                }
                return instance;
            }
        }

        SaveUtility()
        { }

        /// <summary>
        /// セーブデータからロードを行う
        /// </summary>
        /// <param name="savedData">Save()で取得したデータ</param>
        /// <returns>次のセリフのデータ</returns>
        internal NovelData.ParagraphData.Dialogue LoadDialogue(NovelSaveData savedData)
        {
            progress = 0;

            //次のセリフを一番最初のセリフで初期化
            NovelData.ParagraphData.Dialogue first = savedData.novelData.paragraphList[0].dialogueList[0];
            NovelData.ParagraphData.Dialogue data = JsonUtility.FromJson<NovelData.ParagraphData.Dialogue>(JsonUtility.ToJson(first));
            if (data.BGMStyle == SoundStyle.UnChange)
                data.BGMStyle = SoundStyle.Stop;

            if (data.SEStyle == SoundStyle.UnChange)
                data.SEStyle = SoundStyle.Stop;

            if (data.backEffect == Effect.UnChange)
                data.backEffect = Effect.None;

            if (data.DialogueEffect == Effect.UnChange)
                data.DialogueEffect = Effect.None;

            for (int i = 0; i < data.charaEffects.Length; i++)
            {
                if (data.charaEffects[i] == Effect.UnChange)
                    data.charaEffects[i] = Effect.None;
            }

            for (int i = 0; i < savedData.passedParagraphId.Count; i++)
            {
                int index = savedData.passedParagraphId[i];
                NovelData.ParagraphData nowParagraph = savedData.novelData.paragraphList[index];

                //通過したノードのセリフの立ち絵を全て確認する
                for (int j = 0; j < nowParagraph.dialogueList.Count; j++)
                {
                    NovelData.ParagraphData.Dialogue nowDialogue = nowParagraph.dialogueList[j];

                    SaveNext(data, nowDialogue);

                    if (j == savedData.dialogueIndex && i == savedData.passedParagraphId.Count - 1)
                    {
                        break;
                    }

                    progress += (100 / savedData.passedParagraphId.Count) / nowParagraph.dialogueList.Count;
                }
            }

            data.howBack = BackChangeStyle.FadeAll;
            data.backFadeColor = Color.black;
            data.backFadeSpeed = 1.0f;

            progress = 1;
            return data;
        }

        internal NovelSaveData SaveDialogue(NovelData novelData, int paragraphIndex, int dialogueIndex, List<int> passedParagraphIdList, List<string> choiceName, List<string> ParagraphName)
        {
            NovelSaveData savedData = new(novelData, paragraphIndex, dialogueIndex, passedParagraphIdList, choiceName, ParagraphName);
            return savedData;
        }

        internal SkipedData Skip(NovelData novelData, int paragraphIndex, int dialogueIndex, List<int> passedParagraphID, List<string> paragraphName, Sprite nowBack)
        {
            NovelData.ParagraphData nowParagraphData = novelData.paragraphList[paragraphIndex];
            if (dialogueIndex >= nowParagraphData.dialogueList.Count)
                dialogueIndex = nowParagraphData.dialogueList.Count - 1;
            NovelData.ParagraphData.Dialogue first = nowParagraphData.dialogueList[dialogueIndex];
            NovelData.ParagraphData.Dialogue data = JsonUtility.FromJson<NovelData.ParagraphData.Dialogue>(JsonUtility.ToJson(first));
            data.back = nowBack;
            SkipedData skipData = new SkipedData();

            while (true)
            {

                if (nowParagraphData.next == Next.End || (nowParagraphData.next == Next.Continue && nowParagraphData.nextParagraphIndex == -1))
                {
                    skipData.next = Next.End;
                    return skipData;
                }

                foreach (var dialogue in nowParagraphData.dialogueList)
                {
                    SaveNext(data, dialogue);
                }

                if (nowParagraphData.next == Next.Choice)
                {
                    skipData.dialogue = data;
                    skipData.next = Next.Choice;
                    skipData.ParagraphIndex = nowParagraphData.index;
                    return skipData;
                }
                else
                {
                    nowParagraphData = novelData.paragraphList[nowParagraphData.nextParagraphIndex];
                    paragraphName.Add(nowParagraphData.nodeName);
                    passedParagraphID.Add(nowParagraphData.index);
                }
            }
        }


        internal NovelData.ParagraphData.Dialogue SkipNextNode(NovelData novelData, NovelData.ParagraphData nowParagraphData, int dialogueIndex, Sprite nowBack)
        {
            progress = 0;
            if (dialogueIndex >= nowParagraphData.dialogueList.Count)
                dialogueIndex = nowParagraphData.dialogueList.Count - 1;
            NovelData.ParagraphData.Dialogue first = nowParagraphData.dialogueList[dialogueIndex];
            NovelData.ParagraphData.Dialogue data = JsonUtility.FromJson<NovelData.ParagraphData.Dialogue>(JsonUtility.ToJson(first));
            data.back = nowBack;
            if (nowParagraphData.next == Next.End || (nowParagraphData.next == Next.Continue && nowParagraphData.nextParagraphIndex == -1))
            {
                return null;
            }

            for (int i = dialogueIndex; i < nowParagraphData.dialogueList.Count; i++)
            {
                SaveNext(data, nowParagraphData.dialogueList[i]);
                progress += 100 / (nowParagraphData.dialogueList.Count - dialogueIndex);
            }

            if (nowParagraphData.next == Next.Continue)
            {
                SaveNext(data, novelData.paragraphList[nowParagraphData.nextParagraphIndex].dialogueList[0]);
            }

            return data;
        }


        void SaveNext(NovelData.ParagraphData.Dialogue data, NovelData.ParagraphData.Dialogue nowDialogue)
        {
            data.Name = nowDialogue.Name;
            data.text = nowDialogue.text;
            data.howBack = BackChangeStyle.FadeAll;
            data.backFadeColor = Color.black;
            data.backFadeSpeed = 1.0f;

            data.back = nowDialogue.howBack != BackChangeStyle.UnChange ? nowDialogue.back : data.back;

            data.backEffect = nowDialogue.backEffect != Effect.UnChange ? nowDialogue.backEffect : data.backEffect;
            data.backEffectStrength = nowDialogue.backEffectStrength;

            data.DialogueEffect = nowDialogue.DialogueEffect != Effect.UnChange ? nowDialogue.DialogueEffect : data.DialogueEffect;
            data.DialogueEffectStrength = nowDialogue.DialogueEffectStrength;

            if (nowDialogue.changeFont)
            {
                data.changeFont = true;
                data.font = nowDialogue.font;
                data.fontColor = nowDialogue.fontColor;
                data.fontSize = nowDialogue.fontSize;
            }

            if (nowDialogue.changeNameFont)
            {
                data.changeNameFont = true;
                data.nameFont = nowDialogue.nameFont;
                data.nameColor = nowDialogue.nameColor;
            }

            data.SE = nowDialogue.SEStyle != SoundStyle.UnChange ? nowDialogue.SE : data.SE;
            data.SEStyle = nowDialogue.SEStyle != SoundStyle.UnChange ? nowDialogue.SEStyle : data.SEStyle;
            data.SELoop = nowDialogue.SELoop;
            data.SECount = nowDialogue.SECount;
            data.SEFadeTime = nowDialogue.SEFadeTime;
            data.SEEndFadeTime = nowDialogue.SEEndFadeTime;

            data.BGM = nowDialogue.BGMStyle != SoundStyle.UnChange ? nowDialogue.BGM : data.BGM;
            data.BGMStyle = nowDialogue.BGMStyle != SoundStyle.UnChange ? nowDialogue.BGMStyle : data.BGMStyle;
            data.BGMLoop = nowDialogue.BGMLoop;
            data.BGMCount = nowDialogue.BGMCount;
            data.BGMFadeTime = nowDialogue.BGMFadeTime;
            data.BGMEndFadeTime = nowDialogue.BGMEndFadeTime;

            for (int charaIndex = 0; charaIndex < data.charas.Length; charaIndex++)
            {

                if (nowDialogue.howCharas[charaIndex] != CharaChangeStyle.UnChange)
                {
                    data.charas[charaIndex] = nowDialogue.charas[charaIndex];
                    data.howCharas[charaIndex] = CharaChangeStyle.Quick;
                }

                if (nowDialogue.charaEffects[charaIndex] != Effect.UnChange)
                {
                    data.charaEffects[charaIndex] = nowDialogue.charaEffects[charaIndex];
                }
                data.charaEffectStrength[charaIndex] = data.charaEffectStrength[charaIndex];

            }

        }
    }

    /// <summary>
    /// セーブデータのクラス。これをNovelPlayerのLoadメソッドに渡すとデータをロードできます
    /// </summary>
    public class NovelSaveData
    {
        public NovelSaveData(NovelData novelData, int paragraphIndex, int dialogueIndex, List<int> passedParagraphId, List<string> choiceName, List<string> ParagraphName)
        {
            this.novelData = novelData;
            this.paragraphIndex = paragraphIndex;
            this.dialogueIndex = dialogueIndex;
            this.passedParagraphId = new List<int>(passedParagraphId);
            this.ParagraphName = new List<string>(ParagraphName);
            this.choiceName = new List<string>(choiceName);
        }
        public NovelData novelData;
        public int paragraphIndex;
        public int dialogueIndex;
        public List<int> passedParagraphId;
        public List<string> choiceName;
        public List<string> ParagraphName;

    }

    internal struct SkipedData
    {
        public NovelData.ParagraphData.Dialogue dialogue;
        public int ParagraphIndex;
        public Next next;
    }
}
