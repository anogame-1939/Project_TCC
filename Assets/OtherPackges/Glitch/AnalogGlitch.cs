// ※クラス名はそのままでもOK。エディタの CustomEditor もそのまま使えます。:contentReference[oaicite:7]{index=7}
using UnityEngine;

namespace Kino
{
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class AnalogGlitch : MonoBehaviour
    {
        [SerializeField, Range(0, 1)] float _scanLineJitter = 0;
        [SerializeField, Range(0, 1)] float _verticalJump = 0;
        [SerializeField, Range(0, 1)] float _horizontalShake = 0;
        [SerializeField, Range(0, 1)] float _colorDrift = 0;

        [Header("Assign the SAME material as URP Feature (Analog)")]
        public Material targetMaterial; // ← 追加

        float _verticalJumpTime;

        void LateUpdate()
        {
            if (targetMaterial == null) return;

            _verticalJumpTime += Time.deltaTime * _verticalJump * 11.3f;

            var sl_thresh = Mathf.Clamp01(1.0f - _scanLineJitter * 1.2f);
            var sl_disp = 0.002f + Mathf.Pow(_scanLineJitter, 3) * 0.05f;

            targetMaterial.SetVector("_ScanLineJitter", new Vector2(sl_disp, sl_thresh));
            targetMaterial.SetVector("_VerticalJump", new Vector2(_verticalJump, _verticalJumpTime));
            targetMaterial.SetFloat("_HorizontalShake", _horizontalShake * 0.2f);
            targetMaterial.SetVector("_ColorDrift", new Vector2(_colorDrift * 0.04f, Time.time * 606.11f));
        }
        
        public void SetParams(float scan, float vjump, float hshake, float drift)
        {
            _scanLineJitter = scan;
            _verticalJump   = vjump;
            _horizontalShake= hshake;
            _colorDrift     = drift;
        }

    }
}
