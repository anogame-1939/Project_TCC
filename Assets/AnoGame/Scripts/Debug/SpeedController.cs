using UnityEngine;
using Unity.TinyCharacterController.Control;

namespace AnoGame.SLFBDebug
{
    public class SpeedController : MonoBehaviour
    {
        [SerializeField]
        MoveControl moveControl;

        [SerializeField]
        float defaultSpeed;

        void Start()
        {
            if (moveControl != null)
            {
                defaultSpeed = moveControl.MoveSpeed;
            }
        }

        public void ResetSpeed()
        {
            moveControl.MoveSpeed = defaultSpeed;
        }

        public void SpeedUp()
        {
            moveControl.MoveSpeed += 1;
        }

        public void SpeedUpDouble()
        {
            moveControl.MoveSpeed *= 2;
        }
    }
}