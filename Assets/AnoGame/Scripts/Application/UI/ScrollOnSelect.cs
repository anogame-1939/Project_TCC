using UnityEngine;
using UnityEngine.EventSystems;

namespace AnoGame.Application.UI
{
    /// <summary>
    /// このコンポーネントを Button（Selectable）につけると、
    /// 選択（ISelectHandler）が来たときに ContentScrollController.ScrollToChild(...) を呼び出します。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ScrollOnSelect : MonoBehaviour, ISelectHandler
    {
        [Tooltip("選択時に呼び出す ContentScrollController の参照をセットしてください")]
        public ContentScrollController scrollController;

        // 先読みしておく（同じゲームオブジェクトの RectTransform）
        private RectTransform _rect;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
        }

        /// <summary>
        /// 選択されたときに自動で呼ばれる
        /// </summary>
        public void OnSelect(BaseEventData eventData)
        {
            if (scrollController != null)
            {
                scrollController.ScrollToChild(_rect);
            }
            else
            {
                Debug.LogWarning($"ScrollOnSelect: ScrollController が設定されていません。GameObject={gameObject.name}");
            }
        }
    }
}
