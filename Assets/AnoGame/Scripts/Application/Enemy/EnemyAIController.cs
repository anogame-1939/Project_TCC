using UnityEngine;
using UnityEngine.AI;
using AnoGame.Application.Player.Control;
using Codice.Client.BaseCommands.TubeClient; // NOTE:微妙...別のnamespaceがいい

namespace AnoGame.Application.Enmemy.Control
{
    public class EnemyAIController : MonoBehaviour, IForcedMoveController
    {
        // プレイヤーのタグ
        [SerializeField] private string playerTag = "Player";
        
        // 移動アニメーションを切り替える速度のしきい値
        [SerializeField] private float speedThreshold = 0.1f;
        
        // 子オブジェクトにあるアニメーターを取得する場合のインデックス
        [SerializeField] private int animatorChildIndex = 0;
        
        // アニメーター内で設定している Bool パラメータ名
        [SerializeField] private string animatorBoolParam = "IsMove";

        private NavMeshAgent agent;
        private Animator animator;

        private GameObject player;

        void Start()
        {
            // NavMeshAgent の取得
            agent = GetComponentInChildren<NavMeshAgent>();
            
            // 指定した子オブジェクトから Animator を取得
            animator = transform.GetChild(animatorChildIndex).GetComponent<Animator>();
        }

        /// <summary>
        /// Bolt の「On Update」相当
        /// 毎フレームごとに速度をチェックしてアニメーターに反映する
        /// </summary>
        void Update()
        {
            // 1. オブジェクトの速度（NavMeshAgent の速度）を取得し、アニメーターの Bool を設定
            float speed = agent.velocity.magnitude;
            bool isMoving = speed > speedThreshold;
            
            // アニメーション用パラメータをセット
            animator.SetBool(animatorBoolParam, isMoving);
        }

        /// <summary>
        /// Bolt の「On Fixed Update」相当
        /// 物理演算やエージェントの移動更新タイミングに合わせて呼び出される
        /// </summary>
        void FixedUpdate()
        {
            // 2. タグからプレイヤーを探して、その位置を目標地点に設定
            if (player == null)
            {
                player = GameObject.FindWithTag(playerTag);
            }
            
            if (player != null)
            {
                Vector3 targetPosition = player.transform.position;
                agent.SetDestination(targetPosition);

                Debug.Log($"Player position: {targetPosition}");
            }
        }

        public void OnForcedMoveBegin()
        {
            // ここではスクリプト自体を無効化
            this.enabled = false;
            agent.isStopped = true;
        }

        /// <summary>
        /// 強制移動が完了したら呼び出して、PlayerActionControllerを再有効化する
        /// </summary>
        public void OnForcedMoveEnd()
        {
            this.enabled = true;
            agent.isStopped = false;
        }
    }
}