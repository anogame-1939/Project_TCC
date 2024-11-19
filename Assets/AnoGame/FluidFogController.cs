using UnityEngine;

namespace AnoGame
{
    public class FluidFogController : MonoBehaviour
    {
        [Header("Material Reference")]
        public Material fogMaterial;

        [Header("Base Settings")]
        [Range(0, 2)]
        public float fogDensity = 1.0f;
        public Color fogColor = Color.white;

        [Header("Flow Settings")]
        public Vector3 flowDirection = new Vector3(1, 0, 0);
        [Range(0, 2)]
        public float flowSpeed = 1.0f;
        
        [Header("Noise Settings")]
        [Range(0.1f, 10f)]
        public float noiseScale = 2.0f;
        public Vector3 noiseSpeed = new Vector3(0.1f, 0.05f, 0.1f);
        
        [Header("Effect Settings")]
        [Range(0, 2)]
        public float turbulence = 0.5f;
        [Range(0, 1)]
        public float distortionStrength = 0.1f;

        private Vector3 currentOffset;

        void Update()
        {
            if (fogMaterial)
            {
                // 基本設定
                fogMaterial.SetFloat("_FogDensity", fogDensity);
                fogMaterial.SetColor("_FogColor", fogColor);
                
                // フロー設定
                Vector3 normalizedFlow = flowDirection.normalized * flowSpeed;
                fogMaterial.SetVector("_FlowDirection", new Vector4(normalizedFlow.x, normalizedFlow.y, normalizedFlow.z, 0));
                
                // 連続的な時間オフセット
                currentOffset += noiseSpeed * Time.deltaTime;
                fogMaterial.SetFloat("_NoiseScale", noiseScale);
                fogMaterial.SetVector("_NoiseSpeed", new Vector4(currentOffset.x, currentOffset.y, currentOffset.z, 0));
                
                // エフェクト設定
                fogMaterial.SetFloat("_Turbulence", turbulence);
                fogMaterial.SetFloat("_DistortionStrength", distortionStrength);
            }
        }
    }
}