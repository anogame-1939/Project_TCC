using UnityEngine;
using UnityEngine.Rendering;

namespace AnoGame
{

    [RequireComponent(typeof(Camera))]
    public class VolumetricFogEffect : MonoBehaviour
    {
        // インスペクタで設定するパラメータ
        [Header("Fog Settings")]
        public Material fogMaterial;
        [Range(0.0f, 1.0f)]
        public float fogDensity = 0.1f;
        public Color fogColor = new Color(0.8f, 0.8f, 0.8f, 1.0f);
        [Range(16, 128)]
        public int rayMarchSteps = 64;
        
        private Camera cam;
        private CommandBuffer cmd;

        void OnEnable()
        {
            cam = GetComponent<Camera>();
            SetupFogCommand();
        }

        void OnDisable()
        {
            if (cmd != null)
            {
                cam.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, cmd);
                cmd = null;
            }
        }

        void SetupFogCommand()
        {
            if (cmd != null)
            {
                cam.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, cmd);
            }

            cmd = new CommandBuffer();
            cmd.name = "Volumetric Fog";

            // フルスクリーンクワッドの描画
            cmd.Blit(BuiltinRenderTextureType.None, BuiltinRenderTextureType.CameraTarget, fogMaterial);
            
            cam.AddCommandBuffer(CameraEvent.AfterForwardAlpha, cmd);
        }

        void Update()
        {
            // マテリアルのプロパティを更新
            if (fogMaterial != null)
            {
                fogMaterial.SetFloat("_FogDensity", fogDensity);
                fogMaterial.SetColor("_FogColor", fogColor);
                fogMaterial.SetFloat("_Steps", rayMarchSteps);
            }
        }
    }
}
