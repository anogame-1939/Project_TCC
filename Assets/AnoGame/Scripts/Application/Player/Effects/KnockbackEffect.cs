
// Assets/AnoGame/Scripts/Application/Player/Effects/KnockbackEffect.cs
using UnityEngine;

namespace AnoGame.Application.Player.Effects
{
    public class KnockbackEffect : MonoBehaviour
    {
        [SerializeField] private float knockbackForce = 10f;
        private Rigidbody rb;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
        }

        public void ApplyKnockback(Vector3 direction)
        {
            rb.AddForce(direction.normalized * knockbackForce, ForceMode.Impulse);
        }
    }
}
