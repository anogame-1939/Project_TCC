using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Kino
{
    public class KinoGlitchURPFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            public string passName = "KinoGlitchURP";
            public RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingPostProcessing;
            public Material analogMaterial;   // Hidden/Kino/AnalogGlitchURP 用
            public Material digitalMaterial;  // Hidden/Kino/DigitalGlitchURP 用
            public bool runAnalog = true;
            public bool runDigital = true;
        }

        class FullscreenPass : ScriptableRenderPass
        {
            readonly string _tag;
            readonly Material _mat;
            readonly Func<Camera, Material> _materialResolver; // Material resolver
            RTHandle _tmp;

            public FullscreenPass(string tag, Material mat, RenderPassEvent evt, Func<Camera, Material> materialResolver = null)
            {
                _tag = tag;
                _mat = mat;
                _materialResolver = materialResolver;
                renderPassEvent = evt;

                ConfigureInput(ScriptableRenderPassInput.Color);
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                var desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;
                RenderingUtils.ReAllocateIfNeeded(
                    ref _tmp, in desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: $"{_tag}_Tmp"
                );
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                // Material resolution logic
                Material materialToUse = _mat;
                if (_materialResolver != null)
                {
                    var resolved = _materialResolver(renderingData.cameraData.camera);
                    if (resolved != null) materialToUse = resolved;
                }

                if (materialToUse == null) return;

                // ★ Base カメラのみ実行（Overlay で null が出やすい）
                if (renderingData.cameraData.renderType != CameraRenderType.Base) return;

                // （任意）Editor のプレビューや SceneView では実行しない
                // if (renderingData.cameraData.isPreviewCamera || renderingData.cameraData.isSceneViewCamera) return;

                var cmd = CommandBufferPool.Get(_tag);
                var renderer = renderingData.cameraData.renderer;

                // ★ ソース確保（URP16 でまれに null になるケースに備えてフォールバック）
                var src = renderer.cameraColorTargetHandle;
                if (src == null || src.rt == null)
                {
#if UNITY_6000_0_OR_NEWER || UNITY_2022_3 || UNITY_2021_3
                    // URP 16 でも利用可：BackBuffer を取得
                    src = renderer.GetCameraColorBackBuffer(cmd);
#endif
                }
                if (src == null || src.rt == null)
                {
                    // ★ まだ無ければ安全に抜ける（今回の ArgumentNullException 回避ポイント）
                    context.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);
                    return;
                }

                // 以降は今まで通り
                Blitter.BlitCameraTexture(cmd, src, _tmp, materialToUse, 0);
                Blitter.BlitCameraTexture(cmd, _tmp, src);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public override void OnCameraCleanup(CommandBuffer cmd) { }

        }

        public Settings settings = new Settings();
        FullscreenPass _analogPass;
        FullscreenPass _digitalPass;

        public override void Create()
        {
            if (settings.runAnalog)
                _analogPass = new FullscreenPass(settings.passName + "_Analog", settings.analogMaterial, settings.injectionPoint,
                    cam => cam.GetComponent<AnalogGlitch>()?.RuntimeMaterial
                );
            if (settings.runDigital)
                _digitalPass = new FullscreenPass(settings.passName + "_Digital", settings.digitalMaterial, settings.injectionPoint);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.runAnalog && settings.analogMaterial != null) renderer.EnqueuePass(_analogPass);
            if (settings.runDigital && settings.digitalMaterial != null) renderer.EnqueuePass(_digitalPass);
        }
    }
}