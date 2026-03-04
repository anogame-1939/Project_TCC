using UnityEngine;

namespace Localizer
{
    /// <summary>
    /// ローカライズ対象のテキストを持つコンポーネント
    /// </summary>
    public class LocalizeComponent : MonoBehaviour
    {
        [SerializeField] private bool ignore = false;
        public bool Ignore => ignore;
        [SerializeField] private string _originText;
        public string OriginText => _originText;

        public void SetOriginText(string text)
        {
            _originText = text;
        }
    }
}