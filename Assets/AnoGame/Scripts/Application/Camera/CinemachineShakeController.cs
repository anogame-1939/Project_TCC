using UnityEngine;
using Cinemachine;

namespace AnoGame.Application.GameCamera
{
    [RequireComponent(typeof(CinemachineVirtualCamera))]
    public class CinemachineShakeController : MonoBehaviour
    {
        private CinemachineVirtualCamera _virtualCamera;
        private CinemachineBasicMultiChannelPerlin _perlin;

        private void Awake()
        {
            _virtualCamera = GetComponent<CinemachineVirtualCamera>();
            if (_virtualCamera != null)
            {
                _perlin = _virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            }
        }

        public void SetShake(float intensity, float frequency)
        {
            if (_perlin != null)
            {
                _perlin.m_AmplitudeGain = intensity;
                _perlin.m_FrequencyGain = frequency;
            }
        }

        private void OnDisable()
        {
            // Reset shake when disabled to prevent stuck shake
            SetShake(0f, 0f);
        }
    }
}
