using UnityEngine;

namespace AnoGame.Application.Event
{
    public class TimeScaleModifier : MonoBehaviour
    {
        [Tooltip("設定するタイムスケール（例: 0.8 で0.8倍速）")]
        [SerializeField]
        private float targetTimeScale = 0.8f;

        /// <summary>
        /// インスペクターで設定したタイムスケールを適用する
        /// </summary>
        public void ApplyTimeScale()
        {
            Time.timeScale = targetTimeScale;
        }

        /// <summary>
        /// 任意のタイムスケールを適用する
        /// </summary>
        /// <param name="scale">タイムスケール</param>
        public void SetTimeScale(float scale)
        {
            Time.timeScale = Mathf.Max(0f, scale);
        }

        /// <summary>
        /// タイムスケールをリセットする (1.0に戻す)
        /// </summary>
        public void ResetTimeScale()
        {
            Time.timeScale = 1.0f;
        }

        private void OnDisable()
        {
            // オブジェクト無効化時に戻すのが安全かも知れないが、
            // 演出として残したい場合もあるので、今回は自動リセットは行わない方針とする。
            // 必要なら ResetTimeScale をイベントで呼んでもらう。
        }
    }
}
