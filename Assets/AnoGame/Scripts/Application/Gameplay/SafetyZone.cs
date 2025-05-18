using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnoGame.Application.Gameplay
{
    public class SafetyZone : MonoBehaviour
    {
        public string playerTag = "Player";
        // Start is called before the first frame update
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(playerTag))
            {
                GameStateManager.Instance.SetSubState(GameSubState.Safety);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(playerTag))
            {
                GameStateManager.Instance.SetSubState(GameSubState.None);
            }
        }
    }
}