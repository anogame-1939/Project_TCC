using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace NovelEditor
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    internal class NameText : MonoBehaviour
    {
        TextMeshProUGUI tmpro;

        TMP_FontAsset defaultFont;
        float defaultFontSize;
        Color defaultFontColor;

        void Awake()
        {
            tmpro = GetComponent<TextMeshProUGUI>();
            //再生時のフォントを保存
            defaultFont = tmpro.font;
            defaultFontSize = tmpro.fontSize;
            defaultFontColor = tmpro.color;
        }

        internal void DeleteText(){
            tmpro.text = "";
        }

        /// <summary>
        /// ゲーム開始時のフォントに戻す
        /// </summary>
        internal void SetDefaultFont(){
            tmpro.font = defaultFont;
            tmpro.fontSize = defaultFontSize;
            tmpro.color = defaultFontColor;
        }


        /// <summary>
        /// 名前のテキストを設定する
        /// </summary>
        /// <param name="data">次のセリフのデータ</param>
        internal void UpdateNameText(NovelData.ParagraphData.Dialogue data){
            tmpro.text = data.Name;
            if (data.changeNameFont)
            {
                tmpro.color = data.nameColor;

                if (data.nameFont != null)
                    tmpro.font = data.nameFont;
            }
        }
    }
}
