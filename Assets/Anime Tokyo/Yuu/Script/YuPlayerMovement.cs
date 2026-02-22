using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller;
    public float speed = 6.0f;
    public float gravity = -9.81f;
    
    private Vector3 velocity;

    void Update()
    {
        Vector3 moveDirection = Vector3.zero;

        // Wキー: X正方向 / Z負方向
        if (Input.GetKey(KeyCode.W)) moveDirection += new Vector3(1, 0, -1);
        
        // Sキー: X負方向 / Z正方向
        if (Input.GetKey(KeyCode.S)) moveDirection += new Vector3(-1, 0, 1);
        
        // Aキー: X正方向 / Z正方向
        if (Input.GetKey(KeyCode.A)) moveDirection += new Vector3(1, 0, 1);
        
        // Dキー: X負方向 / Z負方向
        if (Input.GetKey(KeyCode.D)) moveDirection += new Vector3(-1, 0, -1);

        // 入力がある場合、正規化して移動速度を一定にする
        if (moveDirection.magnitude > 0.1f)
        {
            controller.Move(moveDirection.normalized * speed * Time.deltaTime);
        }

        // 重力処理
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}