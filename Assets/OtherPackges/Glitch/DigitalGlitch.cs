using UnityEngine;

namespace Kino
{
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class DigitalGlitch : MonoBehaviour
    {
        [SerializeField, Range(0,1)] float _intensity = 0;
        public float intensity { get => _intensity; set => _intensity = value; }

        [Header("Assign the SAME material as URP Feature (Digital)")]
        [SerializeField]
        public Material targetMaterial; // ← 追加

        [SerializeField] Shader _shader;
        Material _material;       // 旧: 自己描画用 → ノイズ生成/保持用に流用
        Texture2D _noiseTexture;
        RenderTexture _trashFrame1;
        RenderTexture _trashFrame2;

        static Color RandomColor() => new Color(Random.value, Random.value, Random.value, Random.value);

        void SetUpResources()
        {
            if (_material != null) return;
            _material = new Material(_shader){ hideFlags = HideFlags.DontSave };

            _noiseTexture = new Texture2D(64, 32, TextureFormat.ARGB32, false) {
                hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point
            };
            _trashFrame1 = new RenderTexture(Screen.width, Screen.height, 0){ hideFlags = HideFlags.DontSave };
            _trashFrame2 = new RenderTexture(Screen.width, Screen.height, 0){ hideFlags = HideFlags.DontSave };

            UpdateNoiseTexture();
        }

        void UpdateNoiseTexture()
        {
            var color = RandomColor();
            for (var y = 0; y < _noiseTexture.height; y++)
            for (var x = 0; x < _noiseTexture.width;  x++)
            {
                if (Random.value > 0.89f) color = RandomColor();
                _noiseTexture.SetPixel(x, y, color);
            }
            _noiseTexture.Apply();
        }

        void Update()
        {
            if (Random.value > Mathf.Lerp(0.9f, 0.5f, _intensity))
            {
                SetUpResources();
                UpdateNoiseTexture();
            }

            if (targetMaterial == null) return;
            SetUpResources();

            // 旧 OnRenderImage の更新頻度を模倣（フレームスキップ）
            var f = Time.frameCount;
            if (f % 13 == 0) Graphics.Blit(Texture2D.blackTexture, _trashFrame1);
            if (f % 73 == 0) Graphics.Blit(Texture2D.blackTexture, _trashFrame2);

            targetMaterial.SetFloat("_Intensity", _intensity);
            targetMaterial.SetTexture("_NoiseTex", _noiseTexture);
            var trashFrame = Random.value > 0.5f ? _trashFrame1 : _trashFrame2;
            targetMaterial.SetTexture("_TrashTex", trashFrame);
        }
    }
}
