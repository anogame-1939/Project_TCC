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

        public void SetAnalogParams(float scan, float vjump, float hshake, float drift)
            => analog?.SetParams(scan, vjump, hshake, drift);

        public void SetDigitalIntensity(float intensity)
            => digital?.SetIntensity(intensity);
            
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