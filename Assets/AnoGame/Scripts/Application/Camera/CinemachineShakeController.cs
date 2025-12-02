using UnityEngine;
using Cinemachine;

namespace AnoGame.Application.GameCamera
{
    [ExecuteAlways]
    [RequireComponent(typeof(CinemachineVirtualCamera))]
    public class CinemachineShakeController : MonoBehaviour
    {
        private CinemachineVirtualCamera _virtualCamera;
        private CinemachineBasicMultiChannelPerlin _perlin;
        private CinemachineCameraOffset _cameraOffset;

        private void OnEnable()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            if (_virtualCamera == null)
                _virtualCamera = GetComponent<CinemachineVirtualCamera>();

            if (_virtualCamera != null)
            {
                if (_perlin == null)
                    _perlin = _virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

                if (_cameraOffset == null)
                    _cameraOffset = _virtualCamera.GetComponent<CinemachineCameraOffset>();
            }
        }

        public void SetShake(float intensity, float frequency)
        {
            if (_perlin == null) InitializeComponents();

            if (_perlin != null)
            {
                _perlin.m_AmplitudeGain = intensity;
                _perlin.m_FrequencyGain = frequency;

                // Verbose log to debug values
                Debug.Log($"[CinemachineShakeController] SetShake: Int={intensity}, Freq={frequency} on {gameObject.name}");
            }
            else
            {
                Debug.LogWarning("[CinemachineShakeController] Perlin is null in SetShake!");
            }
        }

        public void SetOffset(Vector3 offset)
        {
            if (_cameraOffset == null) InitializeComponents();

            if (_cameraOffset != null)
            {
                _cameraOffset.m_Offset = offset;
            }
            else
            {
                Debug.LogWarning("[CinemachineShakeController] CameraOffset is null in SetOffset!");
            }
        }

        private void OnDisable()
        {
            // Reset shake when disabled to prevent stuck shake
            SetShake(0f, 0f);
            SetOffset(Vector3.zero);
        }
    }
}
