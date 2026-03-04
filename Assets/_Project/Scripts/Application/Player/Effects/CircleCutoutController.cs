using UnityEngine;

namespace AnoGame.Application.Player.Effects
{
    /// <summary>
    /// プレイヤー位置を基準とした丸窓切り抜きエフェクトのコントローラ。
    /// Shader.SetGlobal* でポストプロセスシェーダーにプレイヤー座標を送信する。
    /// </summary>
    public class CircleCutoutController : MonoBehaviour
    {
        [Header("対象トランスフォーム")]
        [Tooltip("切り抜き中心となるTransform（通常はプレイヤー）")]
        [SerializeField] private Transform _target;

        [Header("切り抜き設定")]
        [Tooltip("切り抜き半径（ワールド単位）")]
        [SerializeField, Range(0.5f, 20f)] private float _radius = 3f;

        [Tooltip("フェードエッジの幅（ワールド単位）")]
        [SerializeField, Range(0.1f, 5f)] private float _fadeWidth = 1f;

        [Header("有効/無効")]
        [SerializeField] private bool _enabled = true;

        private static readonly int PlayerWorldPosId = Shader.PropertyToID("_PlayerWorldPos");
        private static readonly int CutoutRadiusId = Shader.PropertyToID("_CutoutRadius");
        private static readonly int CutoutFadeWidthId = Shader.PropertyToID("_CutoutFadeWidth");

        /// <summary>切り抜き半径</summary>
        public float Radius
        {
            get => _radius;
            set => _radius = Mathf.Max(0f, value);
        }

        /// <summary>フェードエッジの幅</summary>
        public float FadeWidth
        {
            get => _fadeWidth;
            set => _fadeWidth = Mathf.Max(0.01f, value);
        }

        /// <summary>エフェクトの有効/無効を切り替える</summary>
        public bool IsEnabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        private void LateUpdate()
        {
            if (!_enabled || _target == null)
            {
                // 無効時は半径を0にして切り抜きを行わない
                Shader.SetGlobalFloat(CutoutRadiusId, 0f);
                Shader.SetGlobalFloat(CutoutFadeWidthId, 0f);
                return;
            }

            Vector3 pos = _target.position;
            Shader.SetGlobalVector(PlayerWorldPosId, new Vector4(pos.x, pos.y, pos.z, 0f));
            Shader.SetGlobalFloat(CutoutRadiusId, _radius);
            Shader.SetGlobalFloat(CutoutFadeWidthId, _fadeWidth);
        }

        private void OnDisable()
        {
            // 無効時はエフェクトをリセット
            Shader.SetGlobalFloat(CutoutRadiusId, 0f);
            Shader.SetGlobalFloat(CutoutFadeWidthId, 0f);
        }
    }
}
