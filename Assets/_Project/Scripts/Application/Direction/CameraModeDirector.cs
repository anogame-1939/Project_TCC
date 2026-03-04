using UnityEngine;
using Cinemachine;

namespace AnoGame.Application.Direction
{
    [DisallowMultipleComponent]
    public class CameraModeDirector : MonoBehaviour
    {
        [Header("VCams")]
        [SerializeField] private CinemachineVirtualCamera vcamOrtho; // Lens.Orthographic = true / OrthographicSize を設定
        [SerializeField] private CinemachineVirtualCamera vcamPersp; // Lens.Orthographic = false / FieldOfView を設定

        [Header("Priorities")]
        [SerializeField] private int standbyPriority = 10; // 非アクティブ側
        [SerializeField] private int activePriority  = 20; // アクティブ側

        private void Awake()
        {
        }

        [ContextMenu("Switch → Perspective")]
        public void ToPerspective()
        {
            if (vcamPersp) vcamPersp.Priority = activePriority;
            if (vcamOrtho) vcamOrtho.Priority = standbyPriority;
        }

        [ContextMenu("Switch → Orthographic")]
        public void ToOrthographic()
        {
            if (vcamOrtho) vcamOrtho.Priority = activePriority;
            if (vcamPersp) vcamPersp.Priority = standbyPriority;
        }

        [ContextMenu("Toggle Ortho/Persp")]
        public void Toggle()
        {
            var brain = CinemachineCore.Instance.GetActiveBrain(0);
            var active = brain ? brain.ActiveVirtualCamera : null;
            bool isPerspActive = active != null && ReferenceEquals(
                active.VirtualCameraGameObject, vcamPersp ? vcamPersp.gameObject : null);

            if (isPerspActive) ToOrthographic();
            else               ToPerspective();
        }
    }
}
