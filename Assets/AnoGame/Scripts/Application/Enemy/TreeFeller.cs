using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnoGame.Application.Enemy
{
    public class TreeFeller : MonoBehaviour
    {
        [Header("実行間隔")]
        public float duration = 10f;
        [Header("周囲の木を検出するための半径")]
        public float detectionRadius = 10f;
        [Header("木に加える力の大きさ")]
        public float forceMagnitude = 10f;
        [Header("木の重さ")]
        public float treeMass = 50f;
        [Header("木の重心オフセット")]
        public Vector3 centerOfMass = new Vector3(0f, -2f, 0f);
        
        [Header("重力除去までの時間")]
        public float removeRigitBodyDelay = 1f;

        [Header("倒すアニメーション設定")]
        // 倒れるのにかかる時間
        public float fallDuration = 2f;
        // 何度倒すか(90度で横倒しイメージ)
        public float fallAngle = 90f;

        void Start()
        {
            // 10秒ごとに木を倒す処理を開始するコルーチンを実行
            StartCoroutine(TreeFellingRoutine());
        }

        /// <summary>
        /// 10秒ごとに周囲の木オブジェクトを検出して、倒す処理を行う
        /// </summary>
        IEnumerator TreeFellingRoutine()
        {
            while (true)
            {
                FellTrees();
                yield return new WaitForSeconds(duration);
            }
        }

        /// <summary>
        /// 周囲の木オブジェクトを検出して倒す処理
        /// インスペクター上でコンテキストメニューから実行可能
        /// </summary>
        [ContextMenu("Fell Trees")]
        public void FellTrees()
        {
            Debug.Log("FellTrees: 処理開始");

            // 自身の周囲(detectionRadius)のColliderを取得
            Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius);
            foreach (Collider col in colliders)
            {
                // タグが "Tree" のオブジェクトを対象
                if (col.CompareTag("Tree"))
                {
                    // まだ TreeFallCoroutine が付いていないならアタッチする
                    CustomTreeRigidbody fallScript = col.GetComponent<CustomTreeRigidbody>();
                    if (fallScript == null)
                    {
                        fallScript = col.gameObject.AddComponent<CustomTreeRigidbody>();
                    }

                    // アニメーションのパラメータを設定
                    fallScript.fallDuration = fallDuration;
                    fallScript.fallAngle = fallAngle;

                    // 倒す処理を開始 (引数に“倒す側”の位置を渡す)
                    fallScript.Fall(transform.position);
                }
            }
        }

        /// <summary>
        /// 指定した時間後に対象オブジェクトからRigidbodyを削除し、static状態に戻す
        /// </summary>
        IEnumerator RemoveRigidbodyAfterDelay(GameObject tree, float delay)
        {
            Debug.Log("RemoveRigidbodyAfterDelay: " + tree.name);
            yield return new WaitForSeconds(delay);

            // 追加したRigidbodyがあれば削除
            Rigidbody rb = tree.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Destroy(rb);
            }

            // ※通常はランタイム中にisStaticは変更できませんが、ここでは処理として記述
            tree.isStatic = true;
        }

        // デバッグ用に detectionRadius を描画する
        void OnDrawGizmos()
        {
            // わかりやすいように黄色で描画
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}
