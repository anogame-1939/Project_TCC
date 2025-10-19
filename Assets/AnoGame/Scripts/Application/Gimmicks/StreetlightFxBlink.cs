using UnityEngine;

namespace AnoGame.Application.Gimmicks
{
    public class StreetlightFxBlink : MonoBehaviour
    {
        [Header("Targets")]
        [SerializeField] private Light spotLight;
        [SerializeField] private Renderer bulbRenderer; // 電球メッシュ

        [Header("Emission")]
        [SerializeField] private Color emissionOn  = new Color(1f, 0.85f, 0.6f, 1f);
        [SerializeField] private Color emissionOff = new Color(0.05f, 0.05f, 0.05f, 1f);
        [SerializeField] private float emissionIntensity = 2.0f; // 乗算

        [Header("Spot")]
        [SerializeField] private float spotOnIntensity  = 1.2f;
        [SerializeField] private float spotOffIntensity = 0.0f;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock _mpb;
        private bool _masterEnabled = true;
        private bool _logicalOn = true; // Timeline 等からの論理 ON/OFF

        private void Awake()
        {
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            ApplyVisual();
        }

        public void SetMasterEnabled(bool enabled)
        {
            _masterEnabled = enabled;
            ApplyVisual();
        }

        public void SetLogicalOn(bool on)
        {
            _logicalOn = on;
            ApplyVisual();
        }

        // 0〜1 でブレンド（任意：フェード演出に使える）
        public void SetBlend(float t)
        {
            t = Mathf.Clamp01(t);
            bool effectiveOn = _masterEnabled && (t > 0.001f);

            if (spotLight != null)
            {
                spotLight.enabled = effectiveOn;
                spotLight.intensity = Mathf.Lerp(spotOffIntensity, spotOnIntensity, t);
            }

            if (bulbRenderer != null)
            {
                bulbRenderer.GetPropertyBlock(_mpb);
                var col = Color.Lerp(emissionOff, emissionOn, t) * emissionIntensity;
                _mpb.SetColor(EmissionColorId, col);
                bulbRenderer.SetPropertyBlock(_mpb);
                var mat = bulbRenderer.sharedMaterial;
                if (mat != null) mat.EnableKeyword("_EMISSION");
            }
        }

        private void ApplyVisual()
        {
            float t = (_masterEnabled && _logicalOn) ? 1f : 0f;
            SetBlend(t);
        }
    }
}
