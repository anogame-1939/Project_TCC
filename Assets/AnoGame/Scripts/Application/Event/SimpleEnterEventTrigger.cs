using UnityEngine;

namespace AnoGame.Application.Event
{
    public class SimpleEnterEventTrigger : EventTriggerBase
    {
        [SerializeField]
        private string playerTag = "Player"; // インスペクターでプレイヤーのタグを設定可能に

        private void OnTriggerEnter(Collider other)
        {
            // プレイヤータグを持つオブジェクトとの衝突を検出
            if (other.CompareTag(playerTag))
            {
                OnStartEvent();
            }
        }

        protected override void OnStartEvent()
        {
            base.OnStartEvent();
            Debug.Log("OnStartEvent-SimpleEventTrigger");
        }

        public override void OnFinishEvent()
        {
            base.OnFinishEvent();
            Debug.Log("OnCompleteEvent-SimpleEventTrigger");
        }
    }
}