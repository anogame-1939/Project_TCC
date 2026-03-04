using UnityEngine;

namespace AnoGame.Application.Audio
{
    public class FootstepController : MonoBehaviour
    {
        [SerializeField]
        AudioSource _audioSource;

        public void Step()
        {
            _audioSource.Play();
        }
    }
}
