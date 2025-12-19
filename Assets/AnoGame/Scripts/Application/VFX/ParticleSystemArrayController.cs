using UnityEngine;

namespace AnoGame.Application.VFX
{
    /// <summary>
    /// 複数のパーティクルシステムを一括で操作するコンポーネント
    /// </summary>
    public class ParticleSystemArrayController : MonoBehaviour
    {
        [SerializeField, Header("制御対象のパーティクル")]
        private ParticleSystem[] _particleSystems;

        /// <summary>
        /// 全てのパーティクルを停止する
        /// </summary>
        public void StopAll()
        {
            if (_particleSystems == null) return;

            foreach (var ps in _particleSystems)
            {
                if (ps != null)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }

        /// <summary>
        /// 全てのパーティクルを非アクティブにする
        /// </summary>
        public void DeactivateAll()
        {
            if (_particleSystems == null) return;

            foreach (var ps in _particleSystems)
            {
                if (ps != null)
                {
                    ps.gameObject.SetActive(false);
                }
            }
        }
    }
}
