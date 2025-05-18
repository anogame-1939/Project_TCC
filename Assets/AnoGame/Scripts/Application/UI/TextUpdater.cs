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


        public void UpdateText(string value)
        {
            teptText.text = value;
        }
        // 引数のfloat値を文字列に変換して、テキストに反映するメソッド
        public void UpdatePercentageText(float value)
        {
            int percentageValue = Mathf.FloorToInt(value * 100f);
            teptText.text = percentageValue.ToString();
        }
    }
}