using UnityEngine;
using Unity.TinyCharacterController.Control;

namespace AnoGame.SLFBDebug
{
    public class SpeedController : MonoBehaviour
    {
        [SerializeField]
        MoveControl moveControl;

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