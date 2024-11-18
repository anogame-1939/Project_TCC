using UnityEngine;
using Unity.TinyCharacterController.Control;

namespace AnoGame.Application.Player.Effects
{
    public abstract class PlayerEffectBase : MonoBehaviour
    {
        protected MoveControl moveController;
        protected bool isActive = false;
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
            if (isActive)
            {
                // 既に効果が適用中の場合は、より長い方の時間を採用
                timer = Mathf.Max(timer, duration);
            }
            else
            {
                timer = duration;
                isActive = true;
                OnEffectStart();
            }
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
            OnEffectEnd();
        }

        // 効果開始時の処理
        protected abstract void OnEffectStart();

        // 効果終了時の処理
        protected abstract void OnEffectEnd();
    }
}
