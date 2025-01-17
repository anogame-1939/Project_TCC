using UnityEngine;
using Unity.TinyCharacterController.Control;

namespace AnoGame.Application.SLFBDebug
{
    public class SpeedController : MonoBehaviour
    {
        [SerializeField]
        MoveControl moveControl;

        void Start()
        {

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