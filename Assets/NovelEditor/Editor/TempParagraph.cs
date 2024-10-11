using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NovelEditor;
namespace NovelEditor.Editor
{
    /// <summary>
    /// ParagraphDataをインスペクターに表示するためのクラス
    /// </summary>
    internal class TempParagraph : ScriptableObject
    {
        [HideInInspector] public NovelData.ParagraphData data;
        /// <summary>
        /// data内のDialogueList
        /// </summary>
        public List<NovelData.ParagraphData.Dialogue> dialogueList;
    }
}