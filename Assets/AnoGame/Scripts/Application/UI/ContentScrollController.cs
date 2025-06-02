using UnityEngine;
using UnityEngine.UI;

namespace AnoGame.Application.UI
{
    /// <summary>
    /// Content のサイズを取得し、指定した子要素の位置に応じて
    /// 縦方向の Scrollbar の値を自動計算して設定するクラス
    /// 
    /// ・Scroll View の ScrollRect は先に正しく設定しておくこと
    ///   （Content、Viewport、Scrollbar Vertical の紐付けなど）
    /// ・ボタンなどを選択した際に、この ScrollToChild メソッドを呼ぶことで
    ///   自動で「選択した要素が見える範囲内に来る」ように調整する
    /// </summary>
    public class ContentScrollController : MonoBehaviour
    {
        [Header("Scroll View セットアップ")]

        [Tooltip("Scroll View の Content (RectTransform) を設定します")] 
        [SerializeField] private RectTransform content;

        [Tooltip("Scroll View の Viewport (RectTransform) を設定します")] 
        [SerializeField] private RectTransform viewport;

        [Tooltip("縦スクロール用 Scrollbar コンポーネントを設定します")] 
        [SerializeField] private Scrollbar verticalScrollbar;

        // --------------------------------------------------------------

        /// <summary>
        /// 選択された子要素 (RectTransform) の位置に合わせて
        /// verticalScrollbar.value を更新します。
        /// 
        /// たとえば、Button を押した直後や、
        /// 十字キーで移動した後などにこのメソッドを呼ぶと、
        /// 自動的にスクロールして選択した要素がビューポート内に収まるようになります。
        /// </summary>
        /// <param name="child">Content の直接の子要素として並んでいる RectTransform (例: Button の RectTransform)</param>
        public void ScrollToChild(RectTransform child)
        {
            if (content == null || viewport == null || verticalScrollbar == null || child == null)
            {
                Debug.LogWarning("ContentScrollController: 必要な参照が設定されていないか、child が null です。");
                return;
            }

            // 1) Content の全体の高さを取得
            float contentHeight = content.rect.height;

            // 2) Viewport の高さを取得
            float viewportHeight = viewport.rect.height;

            // 3) 子要素 (child) の位置を取得
            //    child.localPosition.y は、Content のローカル座標系での Y 位置を返します。
            //    VRM の Layout では、子要素は上から下方向に Y が負の値になる場合があるので注意。
            float childLocalY = child.localPosition.y;

            // 4) childLocalY を「Content の一番上からの距離」に変換する
            //    通常、Vertical Layout Group を使っている場合：
            //    Content の Pivot が (0.5, 1) （＝上端を基準）になっている想定。
            //    その場合、child.localPosition.y が 0 のとき子要素は「Content の上端に揃っている」状態。
            //    下に行くほど child.localPosition.y はマイナス方向に増えていきます。
            //
            //    そこで絶対値を取り、"Content の上端から下端までどれだけ離れているか" の距離を算出します。
            float distanceFromTop = Mathf.Abs(childLocalY);

            // 5) 正規化のための母数を計算
            //    contentHeight - viewportHeight は「Content が完全にスクロールされたときに
            //    Scrollbar が一番下を示すために必要な移動量（ピクセル単位）」です。
            float scrollableHeight = contentHeight - viewportHeight;

            //    scrollableHeight が 0 以下の場合は、そもそもスクロール不要（要素が全て表示可能）なので
            //    Scrollbar を一番上にしておけばOK。normalizeHeight は 0 or 1 のどちらかにする。
            if (scrollableHeight <= 0f)
            {
                verticalScrollbar.value = 1f;
                return;
            }

            // 6) distanceFromTop を 0 〜 scrollableHeight の範囲で Clamp しておく
            float clampedDistance = Mathf.Clamp(distanceFromTop, 0f, scrollableHeight);

            // 7) 正規化 ( 0 〜 1 ) する
            //    0 → Content の上端に child が来ている（スクロール不要）
            //    scrollableHeight → Content の最下部に child が来ている（完全にスクロール）
            float normalized = clampedDistance / scrollableHeight;

            // 8) Scrollbar.value に代入（Scrollbar.value: 上が 1, 下が 0 なので 1 - 正規化 に変換）
            float scrollbarValue = 1f - normalized;
            verticalScrollbar.value = scrollbarValue;

            // もし ScrollRect の verticalNormalizedPosition を直接動かしたい場合は、下記のようにもできる
            // var scrollRect = verticalScrollbar.GetComponentInParent<ScrollRect>();
            // if (scrollRect != null)
            // {
            //     scrollRect.verticalNormalizedPosition = scrollbarValue;
            // }
        }
    }
}
