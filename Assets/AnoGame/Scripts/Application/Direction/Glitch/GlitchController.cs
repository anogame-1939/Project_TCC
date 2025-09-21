// Assets/AnoGame/Application/Glitch/GlitchController.cs
using System.Collections.Generic;
using System.Reflection;
using Kino;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace AnoGame.Application.Direction.Glitch
{
    [DisallowMultipleComponent]
    public sealed class GlitchController : MonoBehaviour, IGlitchController
    {
        [Header("参照")]
        [SerializeField] Camera targetCamera;         // 空なら Camera.main
        [SerializeField] Kino.AnalogGlitch analog;    // カメラ上の改修版 AnalogGlitch
        [SerializeField] Kino.DigitalGlitch digital;  // カメラ上の改修版 DigitalGlitch

        // RendererFeature を見つけてランタイムにON/OFF
        KinoGlitchURPFeature _feature;

        void Awake()
        {
            if (!targetCamera) targetCamera = Camera.main;
            _feature = FindFeatureOn(targetCamera);
            // null でも動くように：Featureが無ければ“パラメータ0=オフ”で代替
        }

        static bool TryGetRendererFeature<T>(ScriptableRenderer renderer, out T found)
            where T : ScriptableRendererFeature
        {
            found = null;
            if (renderer == null) return false;

            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            // URPバージョン差異に対応（候補名を順に試す）
            var fi = renderer.GetType().GetField("rendererFeatures", Flags)
                    ?? renderer.GetType().GetField("m_RendererFeatures", Flags);
            if (fi == null) return false;

            var list = fi.GetValue(renderer) as List<ScriptableRendererFeature>;
            if (list == null) return false;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is T t) { found = t; return true; }
            }
            return false;
        }

        KinoGlitchURPFeature FindFeatureOn(Camera cam)
        {
            if (!cam) return null;
            var add = cam.GetUniversalAdditionalCameraData();
            var renderer = add ? add.scriptableRenderer : null;
            if (renderer == null) return null;

            return TryGetRendererFeature<KinoGlitchURPFeature>(renderer, out var f) ? f : null;
        }

        // ====== IGlitchController 実装 ======
        public void SetAnalogEnabled(bool enabled)
        {
            if (_feature) _feature.settings.runAnalog = enabled;
            // フィーチャが無くても“疑似オフ”
            if (analog) analog.enabled = true; // コンポ自体は有効のまま
            if (!enabled && analog) SetAnalogParams(0, 0, 0, 0);
        }

        public void SetDigitalEnabled(bool enabled)
        {
            if (_feature) _feature.settings.runDigital = enabled;
            if (digital) digital.enabled = true;
            if (!enabled && digital) SetDigitalIntensity(0f);
        }

        public void SetAnalogParams(float scanLineJitter, float verticalJump, float horizontalShake, float colorDrift)
        {
            if (!analog) return;
            // AnalogGlitch は targetMaterial に値を流し込む仕組み（あなたの改修版）なので
            // フィールドに直接代入して LateUpdate で Material に反映させる方針でもOKだが、
            // ここでは即時反映のために公開セッタを追加していない前提で、SerializeFieldを直接触るなら
            // Editor/Runtime の都合上リフレクション or 公開setterが必要。
            // ここでは最小：Material へ直書き（AnalogGlitch と同じ式）
            var mat = analog ? analog.targetMaterial : null;
            if (!mat) return;

            float t = Time.time;
            float vjTime = t * verticalJump * 11.3f; // 概ね同等の動きに
            var sl_thresh = Mathf.Clamp01(1.0f - scanLineJitter * 1.2f);
            var sl_disp = 0.002f + Mathf.Pow(scanLineJitter, 3) * 0.05f;

            mat.SetVector("_ScanLineJitter", new Vector2(sl_disp, sl_thresh));
            mat.SetVector("_VerticalJump", new Vector2(verticalJump, vjTime));
            mat.SetFloat("_HorizontalShake", horizontalShake * 0.2f);
            mat.SetVector("_ColorDrift", new Vector2(colorDrift * 0.04f, t * 606.11f));
        }

        public void SetDigitalIntensity(float intensity)
        {
            if (!digital) return;
            digital.intensity = intensity;             // あなたの改修版 API
            if (digital.targetMaterial)
                digital.targetMaterial.SetFloat("_Intensity", intensity);
        }

        public void ApplyAnalogProfile(AnalogGlitchProfile profile)
        {
            if (!profile) return;
            SetAnalogEnabled(profile.enabled);
            SetAnalogParams(profile.scanLineJitter, profile.verticalJump,
                            profile.horizontalShake, profile.colorDrift);
        }

        public void ApplyDigitalProfile(DigitalGlitchProfile profile)
        {
            if (!profile) return;
            SetDigitalEnabled(profile.enabled);
            SetDigitalIntensity(profile.intensity);
            // Noise/Trash は DigitalGlitch 側が自動セット（あなたの実装）でOK
        }
    }
}