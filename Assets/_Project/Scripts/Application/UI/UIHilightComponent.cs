using UnityEngine;
using UnityEngine.EventSystems;

namespace AnoGame.Application.UI
{
    /// <summary>
    /// Selectable な UI オブジェクトにアタッチして、
    /// 選択時／非選択時にハイライト用 GameObject の表示を切り替えます。
    /// </summary>
    public class UIHilightComponent : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        [Header("ハイライト用 GameObject")]
        [Tooltip("選択時に表示／非選択時に非表示にするオブジェクトを指定してください。")]
        [SerializeField]
        private GameObject highlightObject;

        private void Awake()
        {
            // 最初は非表示にしておく
            if (highlightObject != null)
                highlightObject.SetActive(false);
        }

        /// <summary>
        /// EventSystem によってオブジェクトが選択されたときに呼ばれます。
        /// </summary>
        public void OnSelect(BaseEventData eventData)
        {
            if (highlightObject != null)
                highlightObject.SetActive(true);
        }

        /// <summary>
        /// EventSystem によってオブジェクトの選択が外れたときに呼ばれます。
        /// </summary>
        public void OnDeselect(BaseEventData eventData)
        {
            if (highlightObject != null)
                highlightObject.SetActive(false);
        }
    }
}
