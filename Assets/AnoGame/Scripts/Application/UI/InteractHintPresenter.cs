using System.Linq;
using UniRx;
using UnityEngine;
using AnoGame.Application.Player.Interaction;
using AnoGame.Application.UI; // InteractHintFollower

namespace AnoGame.Application.UI
{
    /// <summary>
    /// Interaction 系イベントを受けて、InteractHintFollower を表示/非表示＆ターゲット切替
    /// </summary>
    public class InteractHintPresenter : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private InteractHintFollower follower;
        [SerializeField] private Transform fallbackTarget;              // 失敗時のフォールバック（例: Player）

        [Header("Anchor Search")]
        [Tooltip("Source の Transform 直下から、ここに列挙した名前の子を順に探してアンカーに使います")]
        [SerializeField] private string[] anchorNames = new[] { "UIAnchor", "UiAnchor", "Anchor" };

        private readonly CompositeDisposable _disposables = new();

        private void Reset()
        {
            if (!follower) follower = GetComponentInChildren<InteractHintFollower>(true);
        }

        private void OnEnable()
        {
            // 候補の有無（0↔1）で出し入れ
            MessageBroker.Default
                .Receive<InteractablesNearbyChanged>()
                .Subscribe(e =>
                {
                    Debug.Log($"[UI] InteractablesNearbyChanged: HasAny={e.HasAny}", this);
                    if (!follower) return;
                    if (e.HasAny) follower.ShowHint();
                    else          follower.HideHint();
                })
                .AddTo(_disposables);

            // 最有力フォーカスの変化でターゲットを差し替え
            MessageBroker.Default
                .Receive<InteractionFocusChanged>()
                .Subscribe(e =>
                {
                    if (!follower) return;

                    if (e.Current.HasValue)
                    {
                        // 可能なら Source からアンカーTransformを取得
                        var anchor = ExtractAnchorTransform(e.Current.Value.Source);
                        if (!anchor && fallbackTarget) anchor = fallbackTarget;

                        if (anchor)
                        {
                            follower.SetTarget(anchor);
                            follower.ShowHint();
                        }
                        else
                        {
                            follower.HideHint();
                        }
                    }
                    else
                    {
                        // フォーカスが消えた
                        follower.HideHint();
                    }
                })
                .AddTo(_disposables);
        }

        private void OnDisable() => _disposables.Clear();

        /// <summary>
        /// InteractionOption.Source から UI アンカー Transform を推定
        /// </summary>
        private Transform ExtractAnchorTransform(object source)
        {
            if (source is Component c)
            {
                // 優先：子にアンカー名があればそれ
                var t = c.transform;
                foreach (var name in anchorNames)
                {
                    var child = t.Find(name);
                    if (child) return child;
                }

                // IInteractable を実装していて、WorldUiAnchor(Vector3) があるなら
                // そこにダミーアンカーを出して追従させるのも手ですが、
                // まずは Transform 自身を返すだけで十分に機能します。
                return t;
            }
            return null;
        }
    }
}
