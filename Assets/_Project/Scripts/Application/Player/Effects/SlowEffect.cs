using UnityEngine;
using AnoGame.Application.Player.Control;

namespace AnoGame.Application.Player.Effects
{
    public sealed class SlowEffect : PlayerEffectBase
    {
        [SerializeField] private float slowFactor = 0.5f;
        private PlayerSpeedManager speedManager;

        protected override void Start()
        {
            base.Start();
            speedManager = GetComponentInParent<PlayerSpeedManager>();
        }

        protected override void OnEffectStart()
        {
            if (speedManager != null)
            {
                speedManager.RegisterMultiplier("SlowEffect", slowFactor);
            }
        }

        protected override void OnEffectEnd()
        {
            if (speedManager != null)
            {
                speedManager.UnregisterMultiplier("SlowEffect");
            }
        }
    }
}