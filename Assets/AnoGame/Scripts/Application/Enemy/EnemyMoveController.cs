using UnityEngine;

namespace AnoGame.Application.Enemy
{
    public class EnemyMoveController : MonoBehaviour
    {
        [SerializeField] private float threshold = 0.1f;
        
        private Animator animator;
        private Vector3 previousPosition;

        private void Awake()
        {
            // 0番目の子オブジェクトに Animator がアタッチされている想定
            animator = transform.GetChild(0).GetComponent<Animator>();
            
            // 初期座標を記録
            previousPosition = transform.position;
        }

        private void Update()
        {
            // 現在の座標を小数点第3位で四捨五入
            Vector3 currentPosition = RoundVector3(transform.position, 3);

            // 前回の座標も小数点第3位で四捨五入
            Vector3 prevPosition = RoundVector3(previousPosition, 3);

            // 差分を計算
            Vector3 displacement = currentPosition - prevPosition;
            var distance =  Vector3.Distance(currentPosition, prevPosition);

            // 移動速度を算出 (units/second)
            float speed = displacement.magnitude / Time.deltaTime;

            bool isMoving = speed > threshold;

            Debug.Log($"displacement: {displacement}, displacement: {distance}, Speed: {speed}, IsMoving: {isMoving}"); 
            animator.SetBool("IsMove", isMoving);

            // 今回の座標を次回用に保存（丸め後の値）
            previousPosition = currentPosition;
        }

        /// <summary>
        /// Vector3を小数点第decimals位で四捨五入する
        /// </summary>
        private Vector3 RoundVector3(Vector3 source, int decimals)
        {
            return new Vector3(
                RoundFloat(source.x, decimals),
                RoundFloat(source.y, decimals),
                RoundFloat(source.z, decimals)
            );
        }

        /// <summary>
        /// float値を小数点第decimals位で四捨五入する
        /// </summary>
        private float RoundFloat(float value, int decimals)
        {
            double scale = System.Math.Pow(10, decimals);
            return (float)(System.Math.Round(value * scale) / scale);
        }
    }
}
