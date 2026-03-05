using UnityEngine;

namespace AnoGame.Application.Utils
{
    /// <summary>
    /// 指定したターゲットを追従するが、回転は初期状態を維持するスクリプト
    /// </summary>
    public class FollowTargetNoRotation : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private bool _maintainInitialOffset = true;

        private Vector3 _offset;
        private Quaternion _initialRotation;

        private void Start()
        {
            if (_target == null)
            {
                Debug.LogWarning($"{name}: Target is not assigned.");
                return;
            }

            if (_maintainInitialOffset)
            {
                _offset = transform.position - _target.position;
            }
            else
            {
                _offset = Vector3.zero;
            }

            _initialRotation = transform.rotation;
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            transform.position = _target.position + _offset;
            // transform.rotation = _initialRotation;
        }
    }
}
