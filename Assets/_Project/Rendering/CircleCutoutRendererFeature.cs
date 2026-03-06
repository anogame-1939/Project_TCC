using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AnoGame.Application.Rendering
{
    /// <summary>
    /// 丸窓切り抜き用 RendererFeature（2パス方式）。
    /// Pass1: Wall レイヤーを除外してシーンを背景RTに直接描画
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

        private DrawBackgroundPass _drawBackgroundPass;
        private CircleCutoutRenderPass _cutoutPass;

        public override void Create()
        {
            _drawBackgroundPass = new DrawBackgroundPass
            {
                renderPassEvent = RenderPassEvent.AfterRenderingOpaques
            };
            _cutoutPass = new CircleCutoutRenderPass
            {
                // AfterRenderingTransparents: 全Opaque/Transparent描画完了後に合成を実行
                // BeforeRenderingPostProcessing ではSpeedTree等の一部シェーダが
                // まだ描画されていない場合があるため、より確実なタイミングを使用
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_passMaterial == null) return;
            if (!renderingData.cameraData.camera.CompareTag("MainCamera")) return;

            _drawBackgroundPass.Setup(_cuttableLayerMask);
            renderer.EnqueuePass(_drawBackgroundPass);

            _cutoutPass.Setup(_passMaterial);
            renderer.EnqueuePass(_cutoutPass);
        }

        protected override void Dispose(bool disposing)
        {
            _drawBackgroundPass?.Dispose();
            _cutoutPass?.Dispose();
        }

        /// <summary>
        /// 切り抜き対象レイヤーを除外してシーンを背景RTに直接描画するパス。
        /// </summary>
        private class DrawBackgroundPass : ScriptableRenderPass
        {
            private RTHandle _backgroundRT;
            private RTHandle _backgroundDepthRT;
            private LayerMask _excludeMask;

            private static readonly int BackgroundTexId = Shader.PropertyToID("_CircleCutoutBackground");

            private static readonly ShaderTagId[] ShaderTags = new[]
            {
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly"),
                new ShaderTagId("SRPDefaultUnlit"),
                new ShaderTagId("LightweightForward"),
            };

            public void Setup(LayerMask excludeMask)
            {
                _excludeMask = excludeMask;
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                var desc = renderingData.cameraData.cameraTargetDescriptor;

                var colorDesc = desc;
                colorDesc.depthBufferBits = 0;
                RenderingUtils.ReAllocateIfNeeded(ref _backgroundRT, colorDesc, FilterMode.Bilinear,
                    TextureWrapMode.Clamp, name: "_CircleCutoutBackground");

                var depthDesc = desc;
                depthDesc.colorFormat = RenderTextureFormat.Depth;
                depthDesc.depthBufferBits = 32;
                RenderingUtils.ReAllocateIfNeeded(ref _backgroundDepthRT, depthDesc, FilterMode.Point,
                    TextureWrapMode.Clamp, name: "_CircleCutoutBackgroundDepth");
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var cmd = CommandBufferPool.Get("CircleCutout_DrawBackground");

                // 背景RTをレンダーターゲットに設定してクリア
                CoreUtils.SetRenderTarget(cmd, _backgroundRT, _backgroundDepthRT, ClearFlag.All,
                    renderingData.cameraData.camera.backgroundColor);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                // 対象レイヤーを除外した LayerMask
                int drawMask = ~_excludeMask.value;

                // 描画設定
                var drawingSettings = CreateDrawingSettings(
                    ShaderTags[0], ref renderingData, SortingCriteria.CommonOpaque);
                for (int i = 1; i < ShaderTags.Length; i++)
                {
                    drawingSettings.SetShaderPassName(i, ShaderTags[i]);
                }

                // フィルタリング: Opaque キュー、対象レイヤー除外
                var filteringSettings = new FilteringSettings(
                    RenderQueueRange.opaque, drawMask);

                // シーンを背景RTに描画（Wall なし）
                context.DrawRenderers(
                    renderingData.cullResults, ref drawingSettings, ref filteringSettings);

                // ★ レンダーターゲットをカメラのカラーバッファに戻す
                var cameraTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
                var cameraDepth = renderingData.cameraData.renderer.cameraDepthTargetHandle;
                CoreUtils.SetRenderTarget(cmd, cameraTarget, cameraDepth);

                // グローバルテクスチャとして設定
                cmd.SetGlobalTexture(BackgroundTexId, _backgroundRT);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public void Dispose()
            {
                _backgroundRT?.Release();
                _backgroundDepthRT?.Release();
            }
        }

        /// <summary>
        /// フルスクリーン切り抜きパス。
        /// </summary>
        private class CircleCutoutRenderPass : ScriptableRenderPass
        {
            private Material _material;
            private RTHandle _copiedColor;

            public void Setup(Material material)
            {
                _material = material;
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

                Blitter.BlitCameraTexture(cmd, source, _copiedColor);
                _material.SetTexture("_BlitTexture", _copiedColor);

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
