using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NovelEditor
{
    /// <summary>
    /// 画像にエフェクトをつけるクラス
    /// </summary>
    internal class EffectManager
    {
        static EffectManager instance;
        public static EffectManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new EffectManager();
                }
                return instance;
            }
        }

        Shader None;
        Shader Noise;
        Shader Mosaic;
        Shader GrayScale;
        Shader Sepia;
        Shader Jaggy;
        Shader ChromaticAberration;
        Shader Blur;

        internal EffectManager()
        {
            //必要なエフェクトの読み込み
            None = Resources.Load<Shader>("DefaultEffect");
            Noise = Resources.Load<Shader>("NoiseEffect");
            Mosaic = Resources.Load<Shader>("MosaicEffect");
            GrayScale = Resources.Load<Shader>("GrayScaleEffect");
            Sepia = Resources.Load<Shader>("SepiaEffect");
            Jaggy = Resources.Load<Shader>("JaggyEffect");
            ChromaticAberration = Resources.Load<Shader>("ChromaticEffect");
            Blur = Resources.Load<Shader>("BlurEffect");
        }

        /// <summary>
        /// 指定したImageのエフェクトを初期化する
        /// </summary>
        /// <param name="image">エフェクトを消すImage</param>
        internal void InitMaterial(Image image)
        {
            image.material = new Material(None);
        }

        /// <summary>
        /// 指定したImageから別のImageへシェーダーをコピーする
        /// </summary>
        /// <param name="from">コピー元のImage</param>
        /// <param name="dest">コピー先のImage</param>
        internal void copyShader(Image from, Image dest)
        {
            dest.material = GameObject.Instantiate(from.material);
        }

        /// <summary>
        /// 指定したImageにエフェクトを設定する
        /// </summary>
        /// <param name="image">エフェクトを変えるImage</param>
        /// <param name="effect">どのエフェクトにするか</param>
        /// <param name="strength">エフェクトの強さ</param>
        internal void SetEffect(Image image, Effect effect, float strength)
        {

            switch (effect)
            {
                case Effect.None:
                    image.material.shader = None;
                    break;
                case Effect.Noise:
                    image.material.shader = Noise;
                    break;
                case Effect.Mosaic:
                    image.material.shader = Mosaic;
                    break;
                case Effect.GrayScale:
                    image.material.shader = GrayScale;
                    break;
                case Effect.Sepia:
                    image.material.shader = Sepia;
                    break;
                case Effect.Jaggy:
                    image.material.shader = Jaggy;
                    break;
                case Effect.ChromaticAberration:
                    image.material.shader = ChromaticAberration;
                    break;
                case Effect.Blur:
                    image.material.shader = Blur;
                    break;
            }
            if (image.material.HasProperty("_Strength"))
            {
                image.material.SetFloat("_Strength", strength);
            }
        }

    }

}
