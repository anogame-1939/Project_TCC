using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace NovelEditor
{
    /// <summary>
    /// 背景の切り替えを管理するクラス
    /// </summary>
    internal class NovelBackGround : NovelImage
    {
        private NovelImage _backFade;
        private NovelImage _frontFade;
        private NovelImage _allFade;

        void Awake()
        {
            Init();

            //初期化されていなければ、フェード用のImageを作成する
            if (_backFade == null)
            {
                RectTransform backTransform = GetComponent<RectTransform>();

                RectTransform backObj = new GameObject("backFadePanel", typeof(RectTransform)).GetComponent<RectTransform>();
                backObj.transform.SetParent(this.transform);
                CopyRectTransformSize(backTransform, backObj);
                _backFade = backObj.gameObject.AddComponent<NovelImage>();
                _backFade.HideImage();

                RectTransform frontObj = new GameObject("frontFadePanel", typeof(RectTransform)).GetComponent<RectTransform>();
                frontObj.transform.SetParent(this.transform.parent);
                frontObj.transform.SetSiblingIndex(2);
                CopyRectTransformSize(backTransform, frontObj);
                _frontFade = frontObj.gameObject.AddComponent<NovelImage>();
                _frontFade.HideImage();

                RectTransform allObj = new GameObject("allFadePanel", typeof(RectTransform)).GetComponent<RectTransform>();
                allObj.transform.SetParent(this.transform.parent);
                CopyRectTransformSize(backTransform, allObj);
                _allFade = allObj.gameObject.AddComponent<NovelImage>();
                _allFade.HideImage();
            }

        }

        /// <summary>
        /// RectTransformをコピーする
        /// </summary>
        /// <param name="source">コピー元のRectTransform</param>
        /// <param name="dest">コピー先のRectTransform</param>
        void CopyRectTransformSize(RectTransform source, RectTransform dest)
        {
            dest.anchorMin = source.anchorMin;
            dest.anchorMax = source.anchorMax;
            dest.anchoredPosition = source.anchoredPosition;
            dest.sizeDelta = source.sizeDelta;
            dest.localScale = source.localScale;
            dest.transform.position = source.transform.position;
        }

        /// <summary>
        /// 背景をフェードインさせ、背景を切り替える
        /// </summary>
        /// <param name="data">次のセリフのデータ</param>
        /// <param name="token">使用するCancellationToken</param>
        internal async UniTask<bool> BackFadeIn(NovelData.ParagraphData.Dialogue data, CancellationToken token)
        {
            switch (data.howBack)
            {
                case BackChangeStyle.FadeBack:
                    await FadeIn(_backFade, data.backFadeColor, data.backFadeSpeed, token);
                    break;

                case BackChangeStyle.FadeFront:
                    await FadeIn(_frontFade, data.backFadeColor, data.backFadeSpeed, token);
                    break;

                case BackChangeStyle.FadeAll:
                    await FadeIn(_allFade, data.backFadeColor, data.backFadeSpeed, token);
                    break;
            }
            Change(data.back);
            return true;
        }

        /// <summary>
        /// 背景をフェードアウトさせる
        /// </summary>
        /// <param name="data">次のセリフのデータ</param>
        /// <param name="token">使用するCancellationToken</param>
        internal async UniTask<bool> BackFadeOut(NovelData.ParagraphData.Dialogue data, CancellationToken token)
        {
            switch (data.howBack)
            {
                case BackChangeStyle.FadeBack:
                    await FadeOut(_backFade, data.backFadeColor, data.backFadeSpeed, token);
                    break;

                case BackChangeStyle.FadeFront:
                    await FadeOut(_frontFade, data.backFadeColor, data.backFadeSpeed, token);
                    break;

                case BackChangeStyle.FadeAll:
                    await FadeOut(_allFade, data.backFadeColor, data.backFadeSpeed, token);
                    break;
            }
            return true;
        }

        /// <summary>
        /// 指定したパネルをフェードインさせる
        /// </summary>
        /// <param name="Panel">フェードアウトさせるパネル</param>
        /// <param name="dest">フェードの色</param>
        /// <param name="fadeTime">フェードアウトにかかる時間</param>
        /// <param name="token">使用するCancellationToken</param>
        async UniTask<bool> FadeIn(NovelImage Panel, Color dest, float fadeTime, CancellationToken token)
        {
            Panel.image.sprite = null;
            Color from = new Color(dest.r, dest.g, dest.b, 0);
            await Panel.Fade(from, dest, fadeTime / 2, token);
            return true;
        }

        /// <summary>
        /// 指定したパネルをフェードアウトさせる
        /// </summary>
        /// <param name="Panel">フェードアウトさせるパネル</param>
        /// <param name="from">元の色</param>
        /// <param name="fadeTime">フェードアウトにかかる時間</param>
        /// <param name="token">使用するCancellationToken</param>
        async UniTask<bool> FadeOut(NovelImage Panel, Color from, float fadeTime, CancellationToken token)
        {
            Color dest = new Color(from.r, from.g, from.b, 0);
            await Panel.Fade(from, dest, fadeTime / 2, token);
            return true;
        }

        /// <summary>
        /// 背景をディゾルブで切り替える。エフェクトの切り替えも行う
        /// </summary>
        /// <param name="dissolveTime">切り替えにかかる時間</param>
        /// <param name="sprite">次のSprite</param>
        /// <param name="effect">次の背景のエフェクト</param>
        /// <param name="effectStrength">エフェクトの強さ</param>
        /// <param name="token">使用するCancellationToken</param>
        internal async UniTask<bool> Dissolve(float dissolveTime, Sprite sprite, Effect effect, float effectStrength, CancellationToken token)
        {
            if (image.sprite == null)
            {
                HideImage();
                Change(sprite);
                EffectManager.Instance.SetEffect(image, effect, effectStrength);
                Color from = new Color(_defaultColor.r, _defaultColor.g, _defaultColor.b, 0);
                await Fade(from, _defaultColor, dissolveTime, token);
            }
            else
            {
                _backFade.Change(image.sprite);
                EffectManager.Instance.copyShader(image, _backFade.image);
                _backFade.image.color = image.color;
                Change(sprite);
                EffectManager.Instance.SetEffect(image, effect, effectStrength);
                Color dest = new Color(image.color.r, image.color.g, image.color.b, 0);
                await _backFade.Fade(image.color, dest, dissolveTime, token);
                _backFade.HideImage();
            }
            return true;
        }
    }

}
