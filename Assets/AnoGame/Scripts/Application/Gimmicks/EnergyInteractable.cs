using UnityEngine;
using AnoGame.Application.Interfaces;
using AnoGame.Application.Managers;

namespace AnoGame.Application.Gimmicks
{
    /// <summary>
    /// プレイヤーがインタラクトしてエネルギー供給を要求するためのコンポーネント
    /// IEnergyConsumerと同じオブジェクトまたは親にアタッチして使用する
    /// </summary>
    public class EnergyInteractable : MonoBehaviour
    {
        private IEnergyConsumer _consumer;

        private void Awake()
        {
            _consumer = GetComponent<IEnergyConsumer>();
            if (_consumer == null)
            {
                _consumer = GetComponentInParent<IEnergyConsumer>();
            }
        }

        /// <summary>
        /// 外部（プレイヤーのインタラクションスクリプトなど）から呼ばれる
        /// </summary>
        public void Interact()
        {
            if (_consumer == null)
            {
                Debug.LogWarning("[EnergyInteractable] No IEnergyConsumer found.");
                return;
            }

            if (EnergyManager.Instance == null)
            {
                Debug.LogWarning("[EnergyInteractable] EnergyManager instance is null.");
                return;
            }

            // システムが稼働しているかチェックするのはManagerの責務だが、
            // フィードバックのためにここで確認しても良い

            Debug.Log($"[EnergyInteractable] Requesting power for: {_consumer.DeviceName}");
            EnergyManager.Instance.RequestPower(_consumer);
        }
    }
}
