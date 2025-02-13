using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnoGame.Application.Enemy
{
    public class TreeFeller : MonoBehaviour
    {
        [Header("周囲の木を検出するための半径")]
        public float detectionRadius = 10f;
        [Header("木に加える力の大きさ")]
        public float forceMagnitude = 10f;

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
                Debug.Log("TreeFellingRoutine: ");
                // 自身の周囲（detectionRadius内）のColliderを取得
                Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius);
                foreach (Collider col in colliders)
                {
                    Debug.Log("TreeFeller: " + col.name);
                    // タグが "Tree" のオブジェクトに対して処理
                    if (col.CompareTag("Tree"))
                    {
                        Debug.Log("TreeFeller-Tree: " + col.name);
                        // 既にRigidbodyが追加されていないか確認
                        if (col.GetComponent<Rigidbody>() == null)
                        {
                            // ※通常ランタイム中にisStaticは変更できませんが、ここでは処理として記述
                            col.gameObject.isStatic = false;

                            // Rigidbodyを追加
                            Rigidbody rb = col.gameObject.AddComponent<Rigidbody>();

                            // 自身と木オブジェクトの位置から、木が自身から離れる方向を計算
                            Vector3 direction = (col.transform.position - transform.position).normalized;

                            // 算出した方向にImpulseモードで力を加える
                            rb.AddForce(direction * forceMagnitude, ForceMode.Impulse);

                            // 10秒後にRigidbodyを除去し、再び静的状態に戻す処理を開始
                            StartCoroutine(RemoveRigidbodyAfterDelay(col.gameObject, 10f));
                        }
                    }
                }
                // 10秒ごとにこの処理を実行
                yield return new WaitForSeconds(10f);
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
