using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AnoGame.Application.Rendering
{
    /// <summary>
    /// MainCamera タグのカメラでのみフルスクリーンパスを実行する RendererFeature。
    /// FullScreenPassRendererFeature の代替として使用する。
    /// </summary>
    public class CircleCutoutRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] private Material _passMaterial;
        [SerializeField] private ScriptableRenderPassInput _requirements = ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth;

        private CircleCutoutRenderPass _renderPass;

        public override void Create()
        {
            _renderPass = new CircleCutoutRenderPass
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_passMaterial == null) return;

            // MainCamera タグのカメラでのみ実行
            if (!renderingData.cameraData.camera.CompareTag("MainCamera")) return;

            _renderPass.Setup(_passMaterial, _requirements);
            renderer.EnqueuePass(_renderPass);
        }

        protected override void Dispose(bool disposing)
        {
            _renderPass?.Dispose();
        }

        private class CircleCutoutRenderPass : ScriptableRenderPass
        {
            private Material _material;
            private RTHandle _copiedColor;

            public void Setup(Material material, ScriptableRenderPassInput requirements)
            {
                _material = material;
                ConfigureInput(requirements);
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                var desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;
                RenderingUtils.ReAllocateIfNeeded(ref _copiedColor, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_CircleCutoutCopiedColor");
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (_material == null) return;

                var cmd = CommandBufferPool.Get("CircleCutout");

                var source = renderingData.cameraData.renderer.cameraColorTargetHandle;

                // カラーバッファを一時テクスチャにコピー
                Blitter.BlitCameraTexture(cmd, source, _copiedColor);
                _material.SetTexture("_BlitTexture", _copiedColor);

                // フルスクリーン描画
                CoreUtils.SetRenderTarget(cmd, source);
                CoreUtils.DrawFullScreen(cmd, _material);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public void Dispose()
            {
                _copiedColor?.Release();
            }
        }
    }
}
