using UnityEngine;
using Unity.VisualScripting;

namespace AnoGame
{
    [UnitTitle("Calculate Object Velocity")]
    [UnitCategory("Movement")]
    public class CalculateVelocityNode : Unit
    {
        [DoNotSerialize]
        private Vector3 previousPosition;
        
        [DoNotSerialize]
        public ValueInput gameObjectInput;
        
        [DoNotSerialize]
        public ValueOutput velocityOutput;

        protected override void Definition()
        {
            // GameObjectの入力を定義
            gameObjectInput = ValueInput<GameObject>("GameObject");
            
            // 速度の出力を定義
            velocityOutput = ValueOutput<Vector3>("Velocity", (flow) =>
            {
                var gameObject = flow.GetValue<GameObject>(gameObjectInput);
                Vector3 currentPosition = gameObject.transform.position;
                Vector3 velocity = (currentPosition - previousPosition) / Time.deltaTime;
                previousPosition = currentPosition;
                return velocity;
            });

            // 入力と出力の依存関係を設定
            Requirement(gameObjectInput, velocityOutput);
        }
    }
}
