using UnityEngine;

namespace Localizer
{
    /// <summary>
    /// シーン上のTextMeshProUGUIに翻訳テキストを適用するクラス
    /// </summary>
    public class LocalizeHandler : MonoBehaviour
    {
        void Start()
        {
            // 初期化時にローカライズを適用
            ApplyLocalize();
        }
        
        public void ApplyLocalize()
        {
            LocalizationManager.GetInstance().ApplyLocalize();
        }

    }
}
