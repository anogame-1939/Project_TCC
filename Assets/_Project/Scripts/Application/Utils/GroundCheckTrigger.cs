using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace AnoGame.Application.Utils
{
    public class GroundCheckTrigger : MonoBehaviour
    {
        [SerializeField]
        private GameObject target;

        [SerializeField]
        private LayerMask groundLayer;

        [SerializeField]
        private float maxDistance = 100f;

        [SerializeField]
        private UnityEvent onGroundDetected;

        public void StartChecking()
        {
            StartCoroutine(CheckForGround());
        }

        private IEnumerator CheckForGround()
        {
            while (true)
            {
                if (target == null)
                {
                    yield break;
                }

                if (Physics.Raycast(target.transform.position, Vector3.down, maxDistance, groundLayer))
                {
                    onGroundDetected?.Invoke();
                    yield break;
                }

                yield return null;
            }
        }
    }
}
