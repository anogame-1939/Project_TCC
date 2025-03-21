using TMPro;
using UnityEngine;

namespace AnoGame.Application.UI
{
    public class TextUpdater : MonoBehaviour
    {
        // Textコンポーネント（TEPTEXT相当）をInspectorからアサインできるようにします
        [SerializeField]
        private TMP_Text teptText;

        // Awakeで、Inspectorに設定がない場合、自動で同じGameObjectにあるTextコンポーネントを取得します
        private void Awake()
        {
            if (teptText == null)
            {
                teptText = GetComponent<TMP_Text>();
            }
        }

        // OnValidateはエディタ上で値が変更された際に実行されます
        private void OnValidate()
        {
            teptText = GetComponent<TMP_Text>();
        }

        // 引数のfloat値を文字列に変換して、テキストに反映するメソッド
        public void SetTextFromFloat(float value)
        {
            teptText.text = value.ToString();
        }
    }
}