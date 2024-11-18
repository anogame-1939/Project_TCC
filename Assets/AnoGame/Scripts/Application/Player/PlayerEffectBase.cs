// Assets/AnoGame/Scripts/Application/Player/Effects/Base/PlayerEffectBase.cs
using UnityEngine;

namespace AnoGame.Application.Player.Effects
{
    public abstract class PlayerEffectBase : MonoBehaviour
    {
        protected PlayerController playerController;
        protected bool isActive = false;
        protected float duration;
        protected float timer;

        protected virtual void Start()
        {
            playerController = GetComponent<PlayerController>();
        }

        public virtual void TriggerEffect(float duration)
        {
            this.duration = duration;
            timer = duration;
            isActive = true;
        }

        protected virtual void Update()
        {
            if (!isActive) return;

            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                EndEffect();
            }
        }

        protected virtual void EndEffect()
        {
            isActive = false;
        }
    }
}
