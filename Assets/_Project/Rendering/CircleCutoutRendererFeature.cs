using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AnoGame.Application.Rendering
{
    /// <summary>
    /// 丸窓切り抜き用 RendererFeature（2パス方式）。
    /// Pass1: 対象レイヤーを除外した状態のカラーバッファを背景RTにコピー
    /// Pass2: フルスクリーンパスで切り抜きエリア内を背景RTに差し替え
    /// MainCamera タグのカメラでのみ実行。
    /// </summary>
    public class CircleCutoutRendererFeature : ScriptableRendererFeature
    {
        [Header("マテリアル")]
        [SerializeField] private Material _passMaterial;

        [Header("切り抜き対象レイヤー")]
        [Tooltip("このレイヤーのオブジェクトが丸窓内で透過される")]
        [SerializeField] private LayerMask _cuttableLayerMask;

        private CopyBackgroundPass _copyPass;
        private CircleCutoutRenderPass _cutoutPass;

        public override void Create()
        {
            _copyPass = new CopyBackgroundPass
            {
                renderPassEvent = RenderPassEvent.AfterRenderingOpaques
            };
            _cutoutPass = new CircleCutoutRenderPass
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_passMaterial == null) return;
            if (!renderingData.cameraData.camera.CompareTag("MainCamera")) return;

            _copyPass.Setup();
            renderer.EnqueuePass(_copyPass);

            _cutoutPass.Setup(_passMaterial, _copyPass.BackgroundTexture);
            renderer.EnqueuePass(_cutoutPass);
        }

        protected override void Dispose(bool disposing)
        {
            _copyPass?.Dispose();
            _cutoutPass?.Dispose();
        }

        /// <summary>
        /// 対象レイヤー除外状態のカラーバッファを背景としてコピーするパス。
        /// 現時点のカラーバッファ（Opaque描画後）を一時RTにコピーする。
        /// </summary>
        private class CopyBackgroundPass : ScriptableRenderPass
        {
            private RTHandle _backgroundRT;
            public RTHandle BackgroundTexture => _backgroundRT;

            private static readonly int BackgroundTexId = Shader.PropertyToID("_CircleCutoutBackground");

            public void Setup()
            {
                ConfigureInput(ScriptableRenderPassInput.Color);
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                var desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;
                RenderingUtils.ReAllocateIfNeeded(ref _backgroundRT, desc, FilterMode.Bilinear,
                    TextureWrapMode.Clamp, name: "_CircleCutoutBackground");
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var cmd = CommandBufferPool.Get("CircleCutout_CopyBackground");
                var source = renderingData.cameraData.renderer.cameraColorTargetHandle;

                Blitter.BlitCameraTexture(cmd, source, _backgroundRT);
                cmd.SetGlobalTexture(BackgroundTexId, _backgroundRT);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public void Dispose()
            {
                _backgroundRT?.Release();
            }
        }

        /// <summary>
        /// フルスクリーン切り抜きパス。
        /// 切り抜きエリア内のピクセルを背景RT（対象レイヤーなし）に差し替える。
        /// </summary>
        private class CircleCutoutRenderPass : ScriptableRenderPass
        {
            private Material _material;
            private RTHandle _copiedColor;
            private RTHandle _backgroundRT;

            public void Setup(Material material, RTHandle backgroundRT)
            {
                _material = material;
                _backgroundRT = backgroundRT;
                ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                var desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;
                RenderingUtils.ReAllocateIfNeeded(ref _copiedColor, desc, FilterMode.Bilinear,
                    TextureWrapMode.Clamp, name: "_CircleCutoutCopiedColor");
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (_material == null) return;

                var cmd = CommandBufferPool.Get("CircleCutout_Composite");
                var source = renderingData.cameraData.renderer.cameraColorTargetHandle;

                // カラーバッファをコピー
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
