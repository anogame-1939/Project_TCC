using UnityEngine;
using UniRx;
using AnoGame.Application.Message;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;
using VLB;
using System.Collections.Generic;
using AnoGame.Application.Interfaces;
using AnoGame.Application.Managers;

namespace AnoGame.Application.Lighting
{
    public class SpotlightController : MonoBehaviour, IEnergyConsumer
    {
        [SerializeField] private string lightId;

        // IEnergyConsumer Implementation
        public string DeviceName => $"Spotlight_{lightId}";

        public void OnEnergySupplied()
        {
            // 無期限で点灯
            ActivateForDuration(-1);
        }

        public void OnEnergyCut()
        {
            Deactivate();
        }
        [SerializeField] private Light targetLight;
        [SerializeField] private VolumetricLightBeamAbstractBase[] vlbBeams;
        [SerializeField] private GameObject[] additionalVisuals; // VLB or other visual objects
        [SerializeField] private bool defaultState = false;
        [SerializeField] private float fadeDuration = 0.5f;

        private float originalIntensity;
        private Dictionary<VolumetricLightBeamAbstractBase, float> originalVlbIntensities;
        private CancellationTokenSource cts;

        private void Start()
        {
            // EnergyManagerに登録（存在する場合）
            if (EnergyManager.Instance != null)
            {
                EnergyManager.Instance.Register(this);
            }

            if (targetLight != null)
            {
                originalIntensity = targetLight.intensity;
                // Initialize state
                targetLight.intensity = defaultState ? originalIntensity : 0f;
                targetLight.enabled = defaultState || targetLight.intensity > 0;
            }

            // Initialize VLB
            originalVlbIntensities = new Dictionary<VolumetricLightBeamAbstractBase, float>();
            if (vlbBeams != null)
            {
                foreach (var beam in vlbBeams)
                {
                    if (beam != null)
                    {
                        var intensity = GetVlbIntensity(beam);
                        originalVlbIntensities[beam] = intensity;
                        SetVlbIntensity(beam, defaultState ? intensity : 0f);
                        beam.GenerateGeometry(); // Ensure it updates initially
                    }
                }
            }

            SetVisualsActive(defaultState);


            MessageBroker.Default.Receive<SpotlightActivationMessage>()
                .Where(msg => string.IsNullOrEmpty(msg.LightId) || msg.LightId == lightId)
                .Subscribe(msg =>
                {
                    ActivateForDuration(msg.Duration);
                })
                .AddTo(this);

            MessageBroker.Default.Receive<SpotlightDeactivationMessage>()
                .Where(msg => string.IsNullOrEmpty(msg.LightId) || msg.LightId == lightId)
                .Subscribe(_ =>
                {
                    Deactivate();
                })
                .AddTo(this);
        }

        private void ActivateForDuration(float duration)
        {
            // Cancel any ongoing fade/wait
            cts?.Cancel();
            cts?.Dispose();
            cts = new CancellationTokenSource();

            ActivateRoutine(duration, cts.Token).Forget();
        }

        private void Deactivate()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = new CancellationTokenSource();

            DeactivateRoutine(cts.Token).Forget();
        }

        private async UniTaskVoid ActivateRoutine(float activeDuration, CancellationToken token)
        {
            try
            {
                // Determine current and target VLB intensities for fade-in
                Dictionary<VolumetricLightBeamAbstractBase, float> vlbFadeInStartIntensities = new Dictionary<VolumetricLightBeamAbstractBase, float>();
                Dictionary<VolumetricLightBeamAbstractBase, float> vlbFadeInTargetIntensities = new Dictionary<VolumetricLightBeamAbstractBase, float>();

                if (vlbBeams != null)
                {
                    foreach (var beam in vlbBeams)
                    {
                        if (beam != null && originalVlbIntensities.ContainsKey(beam))
                        {
                            vlbFadeInStartIntensities[beam] = GetVlbIntensity(beam);
                            vlbFadeInTargetIntensities[beam] = originalVlbIntensities[beam];
                        }
                    }
                }

                if (targetLight != null) targetLight.enabled = true;
                SetVisualsActive(true);

                // Fade In (from current intensity to original)
                float lightFadeInStartIntensity = targetLight != null ? targetLight.intensity : 0f;
                await FadeIntensity(lightFadeInStartIntensity, originalIntensity, vlbFadeInStartIntensities, vlbFadeInTargetIntensities, fadeDuration, token);

                // If duration is negative, we stay ON indefinitely (until explicit Deactivate is called)
                if (activeDuration < 0)
                {
                    return;
                }

                // Wait
                await UniTask.Delay(TimeSpan.FromSeconds(activeDuration), cancellationToken: token);

                // Determining fade-out targets and calling generic FadeOut logic
                await DeactivateRoutine(token);
            }
            catch (OperationCanceledException)
            {
                // Operation cancelled, likely by a new activation or deactivation
            }
        }

        private async UniTask DeactivateRoutine(CancellationToken token)
        {
            try
            {
                // Determine current and target VLB intensities for fade-out
                Dictionary<VolumetricLightBeamAbstractBase, float> vlbFadeOutStartIntensities = new Dictionary<VolumetricLightBeamAbstractBase, float>();
                Dictionary<VolumetricLightBeamAbstractBase, float> vlbFadeOutTargetIntensities = new Dictionary<VolumetricLightBeamAbstractBase, float>();

                if (vlbBeams != null)
                {
                    foreach (var beam in vlbBeams)
                    {
                        if (beam != null)
                        {
                            vlbFadeOutStartIntensities[beam] = GetVlbIntensity(beam);
                            vlbFadeOutTargetIntensities[beam] = 0f;
                        }
                    }
                }

                // Fade Out
                float lightFadeOutStartIntensity = targetLight != null ? targetLight.intensity : originalIntensity;
                await FadeIntensity(lightFadeOutStartIntensity, 0f, vlbFadeOutStartIntensities, vlbFadeOutTargetIntensities, fadeDuration, token);

                // Turn off
                if (targetLight != null)
                {
                    targetLight.intensity = 0f;
                    targetLight.enabled = false;
                }
                SetVisualsActive(false);
            }
            catch (OperationCanceledException)
            {
                // Operation cancelled
            }
        }

        private async UniTask FadeIntensity(float lightFrom, float lightTo, Dictionary<VolumetricLightBeamAbstractBase, float> vlbFrom, Dictionary<VolumetricLightBeamAbstractBase, float> vlbTo, float duration, CancellationToken token)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                if (targetLight != null)
                {
                    targetLight.intensity = Mathf.Lerp(lightFrom, lightTo, t);
                }

                if (vlbBeams != null)
                {
                    foreach (var beam in vlbBeams)
                    {
                        if (beam != null && vlbFrom.ContainsKey(beam) && vlbTo.ContainsKey(beam))
                        {
                            float currentVlbIntensity = Mathf.Lerp(vlbFrom[beam], vlbTo[beam], t);
                            SetVlbIntensity(beam, currentVlbIntensity);
                        }
                    }
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            // Finalize intensities
            if (targetLight != null) targetLight.intensity = lightTo;

            if (vlbBeams != null)
            {
                foreach (var beam in vlbBeams)
                {
                    if (beam != null && vlbTo.ContainsKey(beam))
                    {
                        SetVlbIntensity(beam, vlbTo[beam]);
                    }
                }
            }
        }

        private void SetVisualsActive(bool active)
        {
            if (additionalVisuals != null)
            {
                foreach (var visual in additionalVisuals)
                {
                    if (visual != null) visual.SetActive(active);
                }
            }
        }

        private float GetVlbIntensity(VolumetricLightBeamAbstractBase beam)
        {
            if (beam == null) return 0f;
            if (beam is VolumetricLightBeamHD hd) return hd.intensity;
            if (beam is VolumetricLightBeamSD sd) return sd.intensityGlobal;
            return 0f;
        }

        private void SetVlbIntensity(VolumetricLightBeamAbstractBase beam, float value)
        {
            if (beam == null) return;

            if (beam is VolumetricLightBeamHD hd)
            {
                hd.intensity = value;
#if UNITY_EDITOR
                if (!UnityEngine.Application.isPlaying) hd.GenerateGeometry();
                else hd.UpdateAfterManualPropertyChange();
#else
                hd.UpdateAfterManualPropertyChange();
#endif
            }
            else if (beam is VolumetricLightBeamSD sd)
            {
                sd.intensityGlobal = value;
#if UNITY_EDITOR
                if (!UnityEngine.Application.isPlaying) sd.GenerateGeometry();
                else sd.UpdateAfterManualPropertyChange();
#else
                sd.UpdateAfterManualPropertyChange();
#endif
            }
        }

        private void OnDestroy()
        {
            if (EnergyManager.Instance != null)
            {
                EnergyManager.Instance.Unregister(this);
            }

            cts?.Cancel();
            cts?.Dispose();
        }
    }
}
