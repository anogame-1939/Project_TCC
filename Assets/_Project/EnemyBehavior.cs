using UnityEngine;

public class EnemyBehavior : MonoBehaviour
{
    private int hitCount = 0; // 当たり判定のカウント
    public int maxHits = 3;   // 最大の当たり判定数

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("-- Hit count: " + other.name);
        if (other.CompareTag("Weapon")) // 棒オブジェクトに特定のタグを設定しておく
        {
            hitCount++;
            Debug.Log("Hit count: " + hitCount);

            if (hitCount >= maxHits)
            {
                Destroy(gameObject); // 敵オブジェクトを削除
            }
        }
    }
}
