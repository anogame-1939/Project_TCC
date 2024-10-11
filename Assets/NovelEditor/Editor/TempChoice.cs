using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NovelEditor;

namespace NovelEditor.Editor
{
    /// <summary>
    /// ChoiceDataをインスペクターに表示するためのクラス
    /// </summary>
    internal class TempChoice : ScriptableObject
    {
        [HideInInspector] public NovelData.ChoiceData data;

    }
}