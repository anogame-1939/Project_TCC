using UnityEngine;
using UniRx;
using AnoGame.Application.Message;

namespace AnoGame.Application.Player.Interaction
{
    public class SpotlightInteractTrigger : MonoBehaviour
    {
        [Header("Spotlight Settings")]
        [SerializeField] private Vector3 targetPosition;
        [SerializeField] private float radius = 5f;
        [SerializeField] private float duration = 5f;
        [SerializeField] private string lightId;
        [SerializeField] private bool useTransformAsPosition = true;

        public void PublishActivation()
        {
            var pos = useTransformAsPosition ? transform.position : targetPosition;
            
            MessageBroker.Default.Publish(new SpotlightActivationMessage(
                pos,
                radius,
                duration,
                lightId
            ));
            
            Debug.Log($"[SpotlightInteractTrigger] Published activation for ID: {lightId}");
        }

        private void OnDrawGizmosSelected()
        {
            var pos = useTransformAsPosition ? transform.position : targetPosition;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(pos, radius);
        }
    }
}
