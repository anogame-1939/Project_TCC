using UnityEngine;
using Unity.TinyCharacterController.Control;


namespace AnoGame.Application.Player.Effects
{
    public abstract class PlayerEffectBase : MonoBehaviour
    {
        protected MoveControl moveController;
        protected bool isActive = false;
        protected float duration;
        protected float timer;

        protected virtual void Start()
        {
            moveController = GetComponentInParent<MoveControl>();
            
            if (moveController == null)
            {
                Debug.LogError($"MoveControlが親オブジェクトに見つけられません。 {GetType().Name}", this);
            }
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
