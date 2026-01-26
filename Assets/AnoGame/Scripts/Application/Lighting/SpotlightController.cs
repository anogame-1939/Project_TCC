using UnityEngine;
using UniRx;
using AnoGame.Application.Message;

namespace AnoGame.Application.Lighting
{
    public class SpotlightController : MonoBehaviour
    {
        [SerializeField] private string lightId;
        [SerializeField] private Light targetLight;
        [SerializeField] private GameObject[] additionalVisuals; // VLB or other visual objects
        [SerializeField] private bool defaultState = false;

        private void Start()
        {
            SetState(defaultState);

            MessageBroker.Default.Receive<SpotlightActivationMessage>()
                .Where(msg => string.IsNullOrEmpty(msg.LightId) || msg.LightId == lightId)
                .Subscribe(msg =>
                {
                    ActivateForDuration(msg.Duration);
                })
                .AddTo(this);
        }

        private void ActivateForDuration(float duration)
        {
            SetState(true);
            Observable.Timer(System.TimeSpan.FromSeconds(duration))
                .Subscribe(_ => SetState(false))
                .AddTo(this);
        }

        private void SetState(bool active)
        {
            if (targetLight != null) targetLight.enabled = active;
            if (additionalVisuals != null)
            {
                foreach (var visual in additionalVisuals)
                {
                    if (visual != null) visual.SetActive(active);
                }
            }
        }
    }
}
