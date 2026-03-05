using UnityEngine;

namespace AnoGame.Application.Player.Effects
{
    /// <summary>
    /// プレイヤー位置を基準とした丸窓切り抜きエフェクトのコントローラ。
    /// Shader.SetGlobal* でポストプロセスシェーダーにプレイヤーのスクリーン座標を送信する。
    /// マテリアル側の _UseGlobalParams トグルが ON の場合に使用される。
    /// </summary>
    public class CircleCutoutController : MonoBehaviour
    {
        [Header("対象トランスフォーム")]
        [Tooltip("切り抜き中心となるTransform（通常はプレイヤー）")]
        [SerializeField] private Transform _target;

        [Tooltip("ターゲットからのオフセット（ワールド空間）")]
        [SerializeField] private Vector3 _targetOffset;

        [Header("切り抜き設定")]
        [Tooltip("切り抜き半径（スクリーン空間、0〜1。0.5=画面の半分）")]
        [SerializeField, Range(0.01f, 1f)] private float _radius = 0.15f;

        [Tooltip("フェードエッジの幅（スクリーン空間、0〜1）")]
        [SerializeField, Range(0.01f, 0.5f)] private float _fadeWidth = 0.05f;

        [Header("有効/無効")]
        [SerializeField] private bool _enabled = true;

        private static readonly int PlayerScreenPosId = Shader.PropertyToID("_Global_PlayerScreenPos");
        private static readonly int CutoutRadiusId = Shader.PropertyToID("_Global_CutoutRadius");
        private static readonly int CutoutFadeWidthId = Shader.PropertyToID("_Global_CutoutFadeWidth");

        private Camera _camera;

        /// <summary>切り抜き半径（スクリーン空間）</summary>
        public float Radius
        {
            get => _radius;
            set => _radius = Mathf.Clamp(value, 0f, 1f);
        }

        /// <summary>フェードエッジの幅（スクリーン空間）</summary>
        public float FadeWidth
        {
            get => _fadeWidth;
            set => _fadeWidth = Mathf.Max(0.001f, value);
        }

        /// <summary>ターゲットからのオフセット</summary>
        public Vector3 TargetOffset
        {
            get => _targetOffset;
            set => _targetOffset = value;
        }

        /// <summary>エフェクトの有効/無効を切り替える</summary>
        public bool IsEnabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        private void Start()
        {
            _camera = Camera.main;
        }

        private void LateUpdate()
        {
            if (!_enabled || _target == null || _camera == null)
            {
                Shader.SetGlobalFloat(CutoutRadiusId, 0f);
                Shader.SetGlobalFloat(CutoutFadeWidthId, 0f);
                return;
            }

            // ワールド座標 → ビューポート座標（0〜1）
            Vector3 worldPos = _target.position + _targetOffset;
            Vector3 viewportPos = _camera.WorldToViewportPoint(worldPos);

            Shader.SetGlobalVector(PlayerScreenPosId, new Vector4(viewportPos.x, viewportPos.y, 0f, 0f));
            Shader.SetGlobalFloat(CutoutRadiusId, _radius);
            Shader.SetGlobalFloat(CutoutFadeWidthId, _fadeWidth);
        }

        private void OnDisable()
        {
            Shader.SetGlobalFloat(CutoutRadiusId, 0f);
            Shader.SetGlobalFloat(CutoutFadeWidthId, 0f);
        }
    }
}
