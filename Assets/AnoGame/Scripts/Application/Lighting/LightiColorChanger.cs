using System.Collections;
using UnityEngine;

namespace AnoGame.Application.Lighting
{
    public class LightiColorChanger : MonoBehaviour
    {
        [SerializeField]
        Light _targetLight;  // DirectionalLight から Light に変更

        [SerializeField]
        Color _changedColor = Color.white;  // デフォルト値を設定

        [SerializeField]
        float _duration = 1.0f;  // 色変更にかかる時間（秒）

        private Color _startColor;  // 開始色を保存
        private Coroutine _currentColorChange;  // 現在実行中のコルーチンを参照

        void Start()
        {
            if (_targetLight == null)
            {
                Debug.LogError($"対象のライトが設定されていません。: {name}");
                return;
            }
        }

        // 色変更を開始するパブリックメソッド
        public void ChangeColor()
        {
            // 既に色変更中の場合は停止
            if (_currentColorChange != null)
            {
                StopCoroutine(_currentColorChange);
            }

            _startColor = _targetLight.color;  // 現在の色を保存
            _currentColorChange = StartCoroutine(ColorChangeCoroutine());
        }

        // 色を徐々に変更するコルーチン
        private IEnumerator ColorChangeCoroutine()
        {
            float elapsedTime = 0f;

            while (elapsedTime < _duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / _duration;  // 進行度を0-1の範囲で計算

                // 現在の色から目標の色まで線形補間
                _targetLight.color = Color.Lerp(_startColor, _changedColor, t);

                yield return null;  // 次のフレームまで待機
            }

            Debug.Log("色を徐々に変更する");

            // 最終的に確実に目標の色に設定
            _targetLight.color = _changedColor;
            _currentColorChange = null;
        }
    }
}