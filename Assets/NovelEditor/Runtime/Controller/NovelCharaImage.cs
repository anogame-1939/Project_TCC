using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;


namespace NovelEditor
{
    /// <summary>
    /// キャラの切り替えを管理するクラス
    /// </summary>
    internal class NovelCharaImage : NovelImage
    {
        void Awake()
        {
            Init();
        }


        /// <summary>
        /// 次のSpriteに切り替え、キャラをフェードで表示する
        /// </summary>
        /// <param name="sprite">次のSprite</param>
        /// <param name="color">フェード色</param>
        /// <param name="fadeTime">フェードにかかる時間</param>
        /// <param name="token">使用するCancellationToken</param>
        internal async UniTask<bool> DissolveIn(Sprite sprite, Color color, float fadeTime, CancellationToken token)
        {
            if (image.sprite == null)
            {
                Color from = new Color(_defaultColor.r, _defaultColor.g, _defaultColor.b, 0);
                Change(sprite);
                await Fade(from, from, fadeTime / 2, token);
            }
            else
            {
                Color dest = new Color(color.r, color.g, color.b, 0);
                await Fade(image.color, dest, fadeTime / 2, token);
                Change(sprite);
            }

            return true;
        }

        /// <summary>
        /// キャラをフェードで非表示する
        /// </summary>
        /// <param name="color">フェード色</param>
        /// <param name="fadeTime">フェードにかかる時間</param>
        /// <param name="token">使用するCancellationToken</param>
        internal async UniTask<bool> DissolveOut(Sprite sprite, Color color, float fadeTime, CancellationToken token)
        {
            if (image.sprite == null)
            {
                Color from = new Color(_defaultColor.r, _defaultColor.g, _defaultColor.b, 0);
                await Fade(from, from, fadeTime / 2, token);
            }
            else
            {
                Color dest = new Color(color.r, color.g, color.b, 0);
                await Fade(dest, _defaultColor, fadeTime / 2, token);
            }

            return true;
        }
    }
}