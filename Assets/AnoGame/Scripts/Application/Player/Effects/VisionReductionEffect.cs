using UnityEngine;

namespace AnoGame.Application.Player.Effects
{
    public sealed class VisionReductionEffect : PlayerEffectBase
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float normalFOV = 60f;
        [SerializeField] private float reducedFOV = 40f;

        protected override void OnEffectStart()
        {
            playerCamera.fieldOfView = reducedFOV;
        }

        protected override void OnEffectEnd()
        {
            playerCamera.fieldOfView = normalFOV;
        }
    }
}