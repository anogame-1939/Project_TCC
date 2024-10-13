using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace NovelEditor
{
    /// <summary>
    /// 背景、キャラクターなどに使用するImageのコンポーネントの規定クラス
    /// </summary>
    [RequireComponent(typeof(Image))]
    internal class NovelImage : MonoBehaviour
    {
        private Image _image;
        [HideInInspector] public Color _defaultColor;
        private float _defaultAlpha;

        public Image image => _image;

        /// <summary>
        /// ImageのSpriteを変更する。nullなら非表示にする
        /// </summary>
        /// <param name="next">次のSprite</param>
        internal void Change(Sprite next)
        {
            if (next == null)
            {
                _image.sprite = null;
                HideImage();
            }
            else
            {
                _image.sprite = next;
                DisplayImage();
            }
        }

        void Awake()
        {
            Init();
        }

        /// <summary>
        /// 初期化用関数
        /// </summary>
        protected void Init()
        {
            _image = GetComponent<Image>();
            _defaultColor = _image.color;
            if (_defaultColor.a == 0)
            {
                _defaultColor = Color.white;
            }
            EffectManager.Instance.InitMaterial(_image);
            _image.raycastTarget = false;
        }

        /// <summary>
        /// Imageをフェードさせる
        /// </summary>
        /// <param name="from">元の色</param>
        /// <param name="dest">目標の色</param>
        /// <param name="fadeTime">フェードにかかる時間</param>
        /// <param name="token">使用するCancellationToken</param>
        internal async UniTask<bool> Fade(Color from, Color dest, float fadeTime, CancellationToken token)
        {
            float alpha = 0;
            _image.color = from;

            float alphaSpeed = 0.01f;
            if (fadeTime < 0.5)
            {
                alphaSpeed = 0.1f;
            }
            try
            {
                while (alpha < 1)
                {
                    _image.color = Color.Lerp(from, dest, alpha);
                    await UniTask.Delay(TimeSpan.FromSeconds(fadeTime * alphaSpeed), cancellationToken: token);
                    alpha += alphaSpeed;
                }
            }
            catch (OperationCanceledException)
            {
                //return false;
            }

            _image.color = dest;
            return true;
        }

        /// <summary>
        /// Imageを非表示にする
        /// </summary>
        internal void HideImage()
        {
            _image.color = new Color(_image.color.r, _image.color.g, _image.color.b, 0);
        }

        /// <summary>
        /// Imageを表示する
        /// </summary>
        internal void DisplayImage()
        {
            _image.color = new Color(_image.color.r, _image.color.g, _image.color.b, 1);
        }


    }

}
