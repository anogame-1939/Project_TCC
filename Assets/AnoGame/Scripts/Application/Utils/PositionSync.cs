using UnityEngine;

namespace AnoGame.Application.Utils
{
    /// <summary>
    /// 指定したTargetのPositionのみを同期するコンポーネント
    /// Editor上でも動作します
    /// </summary>
    [ExecuteAlways]
    public class PositionSync : MonoBehaviour
    {
        [SerializeField] private Transform _target;

        private void LateUpdate()
        {
            if (_target == null) return;

            transform.position = _target.position;
        }
    }
}
