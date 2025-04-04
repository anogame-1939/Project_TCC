    using UnityEngine;
namespace AnoGame.Application.Enemy
{
    [CreateAssetMenu(fileName = "PartialFadeSettings", menuName = "ScriptableObjects/PartialFadeSettings", order = 1)]
    public class PartialFadeSettings : ScriptableObject
    {
        [Range(0f, 1f)]
        public float targetAlpha = 0.5f;
        
        public float duration = 1f;
    }

}