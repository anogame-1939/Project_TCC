using UnityEngine;

public class StickBehavior2 : MonoBehaviour
{
    public float rotationAngle = 45f; // 叩く際の回転角度
    public float rotateSpeed = 10f;   // 回転速度
    private Quaternion initialRotation;  // 初期の回転角度
    private bool isRotating = false;     // 回転中かどうか
    private Quaternion targetRotation;   // 目標の回転角度

    void Start()
    {
        // 初期の回転を保存
        initialRotation = transform.localRotation;
    }

    void Update()
    {
        if (isRotating)
        {
            // 叩く動き
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, targetRotation, rotateSpeed * Time.deltaTime);

            if (Quaternion.Angle(transform.localRotation, targetRotation) < 0.1f)
            {
                // 回転が完了したら元の位置に戻す
                if (targetRotation == initialRotation)
                {
                    isRotating = false;
                }
                else
                {
                    targetRotation = initialRotation; // 元の位置に戻るための目標回転
                }
            }
        }
    }

    public void Attack()
    {
        Debug.Log("Stick clicked!"); // デバッグログを追加
        if (!isRotating)
        {
            // 叩くための回転を設定
            targetRotation = initialRotation * Quaternion.Euler(rotationAngle, 0, 0);
            isRotating = true; // 回転を開始
        }
    }

    void OnMouseDown()
    {
        Attack(); // クリックでAttackメソッドを呼び出し
    }
}
