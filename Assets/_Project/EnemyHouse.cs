using UnityEngine;
using System.Collections.Generic;

public class EnemyHouse : MonoBehaviour
{
    public List<GameObject> objectsToShow; // 表示するGameObjectのリスト

    void Start()
    {
        // リストにある全てのGameObjectを非表示に設定
        foreach (GameObject obj in objectsToShow)
        {
            if (obj != null)
            {
                obj.SetActive(false); // GameObjectを非表示
            }
        }
    }
    void OnTriggerEnter(Collider other)
    {
        // 親オブジェクトがプレイヤーかどうかをチェック
        if (other.transform.parent != null && other.transform.parent.CompareTag("Player"))
        {
            Debug.Log("Player's parent detected!");

            // リストにある全てのGameObjectを表示
            foreach (GameObject obj in objectsToShow)
            {
                if (obj != null)
                {
                    obj.SetActive(true); // GameObjectを表示
                    Debug.Log(obj.name + " is now active.");
                }
            }
        }
    }
}
