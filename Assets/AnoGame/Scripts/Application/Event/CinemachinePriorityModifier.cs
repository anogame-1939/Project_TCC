using UnityEngine;
using Cinemachine;

namespace AnoGame.Application.Event
{
    public class CinemachinePriorityModifier : MonoBehaviour
    {
        [Tooltip("操作対象の仮想カメラ")]
        [SerializeField]
        private CinemachineVirtualCamera targetVirtualCamera;

        [Tooltip("優先度を上げる際の値")]
        [SerializeField]
        private int highPriority = 100;

        private int _originalPriority;
        private bool _isModified = false;

        private void Reset()
        {
            targetVirtualCamera = GetComponent<CinemachineVirtualCamera>();
        }

        private void Awake()
        {
            if (targetVirtualCamera == null)
            {
                targetVirtualCamera = GetComponent<CinemachineVirtualCamera>();
            }
        }

        /// <summary>
        /// 優先度を上げてカメラをアクティブにする
        /// </summary>
        public void SetHighPriority()
        {
            if (targetVirtualCamera == null) return;

            // すでに変更済みでなければ、元の値を保存する
            if (!_isModified)
            {
                _originalPriority = targetVirtualCamera.Priority;
                _isModified = true;
            }

            targetVirtualCamera.Priority = highPriority;
        }

        /// <summary>
        /// 優先度を元に戻す
        /// </summary>
        public void ResetPriority()
        {
            if (targetVirtualCamera == null || !_isModified) return;

            targetVirtualCamera.Priority = _originalPriority;
            _isModified = false;
        }
    }
}
