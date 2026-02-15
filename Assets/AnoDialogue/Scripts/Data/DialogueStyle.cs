using UnityEngine;
using System.Collections.Generic;

namespace AnoGame.AnoDialogue.Data
{
    /// <summary>
    /// スタイルごとのオプション設定。
    /// </summary>
    [System.Serializable]
    public class StyleOption
    {
        public string styleName;
        [Tooltip("このスタイルでSceneImage（ロケーション画像/背景）を使用するか")]
        public bool supportsSceneImage = true;
    }

    [CreateAssetMenu(fileName = "DialogueStyleValues", menuName = "AnoGame/AnoNarrative/Dialogue Style Values")]
    public class DialogueStyle : ScriptableObject
    {
        [Tooltip("Define the available style names here (e.g., 'Standard', 'Narration', 'Flashback')")]
        public List<string> styleNames = new List<string>() { "Standard" };

        [Tooltip("各スタイルのオプション設定")]
        public List<StyleOption> styleOptions = new List<StyleOption>();

        /// <summary>
        /// 指定スタイルがSceneImageをサポートするか判定する。
        /// styleOptionsに未登録の場合はデフォルトで true を返す。
        /// </summary>
        public bool GetSupportsSceneImage(string styleName)
        {
            if (string.IsNullOrEmpty(styleName)) return true;

            foreach (var opt in styleOptions)
            {
                if (opt.styleName == styleName)
                {
                    return opt.supportsSceneImage;
                }
            }

            // 未登録はデフォルト: サポートする
            return true;
        }
    }
}
