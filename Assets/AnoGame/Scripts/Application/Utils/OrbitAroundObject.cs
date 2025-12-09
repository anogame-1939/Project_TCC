using UnityEngine;
using DG.Tweening;

namespace AnoGame.Application.Utils
{
    /// <summary>
    /// 指定したターゲットの周りをぐるぐる回るスクリプト
    /// </summary>
    public class OrbitAroundObject : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Transform _target;
        [SerializeField] private float _radius = 5f; // 半径指定を追加
        [SerializeField] private float _duration = 5f;
        [SerializeField] private Vector3 _axis = Vector3.up;
        [SerializeField] private Ease _ease = Ease.Linear;
        [SerializeField] private bool _autoPlay = true;

        private Tween _orbitTween;
        private Vector3 _initialOffset;

        private void Start()
        {
            if (_autoPlay)
            {
                StartOrbit();
            }
        }

        [Button]
        public void StartOrbit()
        {
            if (_target == null)
            {
                Debug.LogWarning("Target is not assigned in OrbitAroundObject.", this);
                return;
            }

            // 現在のターゲットからの方向を取得
            Vector3 direction = (transform.position - _target.position).normalized;

            // もしターゲットと重なっていて方向が取れない場合は、とりあえずX軸方向とする
            if (direction == Vector3.zero)
            {
                direction = Vector3.right; // Axis等に応じて変えるのが理想だが一旦右で
            }

            // 半径を適用して初期オフセットを決定
            _initialOffset = direction * _radius;

            // オブジェクトの位置を強制的に半径の位置に合わせる
            transform.position = _target.position + _initialOffset;

            StopOrbit();

            // 0度から360度まで回転
            // SetLink(gameObject)でGameObjectが破棄されたらTweenも破棄されるようにする
            _orbitTween = DOVirtual.Float(0f, 360f, _duration, UpdatePosition)
                .SetLoops(-1, LoopType.Restart)
                .SetEase(_ease)
                .SetLink(gameObject);
        }

        private void UpdatePosition(float angle)
        {
            if (_target == null) return;

            // アングルに応じた回転を作成
            Quaternion rotation = Quaternion.AngleAxis(angle, _axis);

            // ターゲットの位置 + 回転させたオフセット
            transform.position = _target.position + (rotation * _initialOffset);
        }

        public void StopOrbit()
        {
            if (_orbitTween != null && _orbitTween.IsActive())
            {
                _orbitTween.Kill();
            }
        }

        private void OnDisable()
        {
            StopOrbit();
        }

        private void OnDestroy()
        {
            StopOrbit();
        }

        // 半径が変更されたときにエディタ上で反映されるように（オプション）
#if UNITY_EDITOR
        private void OnValidate()
        {
            // 実行中はTweenが制御しているのでいじらない
            if (UnityEngine.Application.isPlaying || _target == null) return;

            // エディタで位置調整用（任意）
            // これを入れるとインスペクタでRadiusをいじった瞬間に位置が飛ぶので、
            // 好みによるが、わかりやすさのためには良いかも。
            // ただし、StartOrbitのロジックと重複するので、今回はStartOrbitでの補正のみを主機能とする。
        }
#endif
    }
}
