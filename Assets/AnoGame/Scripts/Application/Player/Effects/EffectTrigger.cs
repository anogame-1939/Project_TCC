using UnityEngine;

namespace AnoGame.Application.Player.Effects
{
    public class EffectTrigger : MonoBehaviour
    {
        [SerializeField] private CollisionType effectType;
        [SerializeField] private bool isOneTimeOnly;
        
        private bool _hasTriggered;

        private void OnTriggerEnter(Collider other)
        {
            if (isOneTimeOnly && _hasTriggered) return;
            
            if (other.CompareTag("Player"))
            {
                if (other.TryGetComponent<EffectManager>(out var effectManager))
                {
                    effectManager.ApplyEffect(effectType);
                    _hasTriggered = true;
                }
            }
        }
    }
}