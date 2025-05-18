using UnityEngine;

public class EnemyFollow : MonoBehaviour
{
    public Transform target; // 追跡対象となるプレイヤー
    public float speed = 2.0f; // エネミーの移動速度

    void Update()
    {
        if (target != null)
        {
            // プレイヤーの方向を計算
            Vector3 direction = target.position - transform.position;
            direction.Normalize();

            // エネミーをプレイヤーに向かって移動
            transform.position += direction * speed * Time.deltaTime;

            // プレイヤーの方向を向く
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * speed);
        }
    }
}
