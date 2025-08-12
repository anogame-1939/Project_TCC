using UnityEngine;
using UnityEngine.UI;

namespace AnoGame.Application.UI
{
    /// <summary>
    /// Scroll View の Content（Pivot＝上端）に対して、
    /// 子要素（Button など）の位置に応じた縦スクロールバー値を計算・設定するクラス。
    ///
    /// ■ 前提
    /// - Content の RectTransform.Pivot が (0.5, 1) になっていること。
    /// - 子要素（Button など）の RectTransform は、
    ///   Vertical Layout Group や手動で縦に並べられているものとする。
    /// - Scroll View（ScrollRect）は次のように設定済み：
    ///   Content → Viewport → Content を Content に登録
    ///   Viewport → Viewport に登録
    ///   VerticalScrollBar → Scrollbar Vertical に登録
    ///   Movement Type は Clamped / Elastic のいずれか
    ///   Vertical のみ有効化、Horizontal は無効化
    /// </summary>
    public class ContentScrollController : MonoBehaviour
    {
        [Header("Scroll View セットアップ")]

        [Tooltip("Scroll View の Content (RectTransform) をセットしてください。\nPivot が (0.5, 1) (上端中央) になっている必要があります。")]
        [SerializeField] private RectTransform content;

        [Tooltip("Scroll View の Viewport (RectTransform) をセットしてください。")]
        [SerializeField] private RectTransform viewport;

        [Tooltip("縦スクロール用の Scrollbar コンポーネントをセットしてください。")]
        [SerializeField] private Scrollbar verticalScrollbar;


        /// <summary>
        /// 選択中の子要素（RectTransform）の位置を元に、縦スクロールバーの値を更新します。
        /// 
        /// ★ Content の Pivot が (0.5, 1) になっている場合の計算例です。
        /// 
        /// 1. Content 全体の高さを取得
        /// 2. Viewport（表示領域）の高さを取得
        /// 3. 子要素 child の「上端からの距離」を計算
        ///    → child.anchoredPosition.y は「Content のピボット(上端)から
        ///      child のピボット(中心)までの距離」を表します。（下方向に移動するとマイナスになる）
        ///    → 子要素の上端： child の中心 y 位置 ＋ (childHeight * (1 - child.pivot.y))
        ///      （child.pivot.y が 0.5 の場合は childHeight * 0.5）
        ///    → つまり、(‐anchoredPosition.y) - (childHeight * (1 - child.pivot.y)) が
        ///      「Content‐上端」から「child の上端」までのピクセル距離になります。
        /// 
        /// 4. スクロール可能な範囲 height (scrollableHeight) を計算 = (contentHeight - viewportHeight)
        /// 5. その範囲を 0～1 に正規化して、verticalScrollbar.value = 1 - normalizedDistance とする。
        ///    → normalizedDistance = clamp( 子要素上端の距離 / scrollableHeight, 0, 1 )
        ///    → 1 - normalizedDistance にすることで、上端を選んだら value＝1、下端を選んだら value＝0 となる。
        /// </summary>
        /// <param name="child">Content の子要素の RectTransform（Button など）</param>
        public void ScrollToChild(RectTransform child)
        {
            if (content == null || viewport == null || verticalScrollbar == null || child == null)
            {
                Debug.LogWarning("ContentScrollController: 参照が不足しています。Content, Viewport, Scrollbar, または child が null です。");
                return;
            }

            // --- 1) Content 全体の高さを取得 ---
            float contentHeight = content.rect.height;

            // --- 2) Viewport の高さを取得 ---
            float viewportHeight = viewport.rect.height;

            // --- 3) 子要素 child の「上端からの距離」を計算 ---
            //
            // childPivotToContentPivotY = child.anchoredPosition.y
            //   ･･･ Content のピボット(上端) から child のピボット(中心) までの距離（下方向ほどマイナス）
            //
            // child の上端位置 = childPivotToContentPivotY -( childHeight * (child.pivot.y - 1) )
            //  ただし child.pivot.y - 1 は常に (0.5 - 1) = -0.5 など負になるので、
            //  childHeight * (1 - child.pivot.y) として式を整理するとわかりやすい：
            //
            // childTopFromContentTop = -( child.anchoredPosition.y ) - ( child.rect.height * (1 - child.pivot.y) )
            //
            // 例:
            //  - child.pivot.y = 0.5（中心）なら (1 - 0.5) = 0.5。つまり上端 = 中心 y + childHeight*0.5
            //  - child.anchoredPosition.y が -20 の場合 (下方向 20px)、childPivotToContentPivotY = -20
            //    → childTop = -(-20) - ( childHeight*0.5 ) = 20 - (childHeight*0.5)
            //    これが「Content の上端」から child の上端までの距離となる。
            //
            float childCenteredY = child.anchoredPosition.y;
            float childHalfHeightFromPivotUp = child.rect.height * (1f - child.pivot.y);
            float distanceFromContentTop = Mathf.Max(0f,
                -childCenteredY - childHalfHeightFromPivotUp
            );

            // --- 4) スクロール可能な高さを計算 ---
            float scrollableHeight = contentHeight - viewportHeight;

            // Content の高さが Viewport 以下(スクロール不要)なら、一番上の状態にする
            if (scrollableHeight <= 0f)
            {
                verticalScrollbar.value = 1f;
                return;
            }

            // --- 5) 正規化 (0～1) ---
            float clampedDistance = Mathf.Clamp(distanceFromContentTop, 0f, scrollableHeight);
            float normalized = clampedDistance / scrollableHeight;

            // --- 6) Scrollbar.value を設定 (上端:1, 下端:0) ---
            float scrollbarValue = 1f - normalized;
            verticalScrollbar.value = scrollbarValue;
        }
    }
}
