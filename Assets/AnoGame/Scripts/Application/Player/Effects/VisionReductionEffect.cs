// Assets/AnoGame/Scripts/Application/Player/Effects/VisionReductionEffect.cs
using UnityEngine;

namespace AnoGame.Application.Player.Effects
{
    public class VisionReductionEffect : PlayerEffectBase
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float normalFOV = 60f;
        [SerializeField] private float reducedFOV = 40f;

        public override void TriggerEffect(float duration)
        {
            base.TriggerEffect(duration);
            playerCamera.fieldOfView = reducedFOV;
        }

        protected override void EndEffect()
        {
            base.EndEffect();
            playerCamera.fieldOfView = normalFOV;
        }
    }
}
