using Unity.TinyCharacterController.Interfaces.Components;
using Unity.TinyCharacterController.Utility;
#if PACKAGE_VISUAL_SCRIPTING
using Unity.VisualScripting;
#endif
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.TinyCharacterController.Ik
{
    [DisallowMultipleComponent]
    [AddComponentMenu(MenuList.Ik + nameof(LookAtRig))]
#if PACKAGE_VISUAL_SCRIPTING
    [RenamedFrom("TinyCharacterController.LookAtRig")]
#endif
#if PACKAGE_VISUAL_SCRIPTING
    [RenamedFrom("TinyCharacterController.Ik.LookAtRig")]
#endif
    public class LookAtRig : MonoBehaviour, IIkRig
    {
        /// <summary>
        /// If true, the character looks at the target.
        /// </summary>
        [FormerlySerializedAs("isWork")] 
#if PACKAGE_VISUAL_SCRIPTING
        [RenamedFrom("isWork")]
#endif
        public bool IsWork ;

        /// <summary>
        /// The object to look at.
        /// </summary>
#if PACKAGE_VISUAL_SCRIPTING
        [AllowsNull]
#endif
#if PACKAGE_VISUAL_SCRIPTING
        [RenamedFrom("target")]
#endif
        [FormerlySerializedAs("target")]
        public Transform Target;
        
        /// <summary>
        /// Time to switch the look-at effect on/off.
        /// </summary>
        [FormerlySerializedAs("transitionTime")] 
#if PACKAGE_VISUAL_SCRIPTING
        [RenamedFrom("transitionTime")]
#endif
        public float TransitionTime = 1;

        [Range(0, 1)] 
        [FormerlySerializedAs("bodyWeight")] 
#if PACKAGE_VISUAL_SCRIPTING
        [RenamedFrom("bodyWeight")]
#endif
        public float BodyWeight = 0.12f;
        
        [Range(0, 1)] 
        [FormerlySerializedAs("headWeight")] 
#if PACKAGE_VISUAL_SCRIPTING
        [RenamedFrom("headWeight")]
#endif
        public float HeadWeight = 0.3f;
        
        [Range(0, 1)] 
        [FormerlySerializedAs("eyeWeight")]
#if PACKAGE_VISUAL_SCRIPTING
        [RenamedFrom("eyeWeight")]
#endif
        public float EyeWeight = 0.3f;
        
        [Range(0, 1)]
        [FormerlySerializedAs("clampAngle")]
#if PACKAGE_VISUAL_SCRIPTING
        [RenamedFrom("clampAngle")]
#endif
        public float ClampAngle = 0.5f;

        private bool _isValid;
        private float _weight;
        private Animator _animator;


        bool IIkRig.IsValid => Target != null;
        float IIkRig.Weight => _weight;

        public void Initialize(Animator animator)
        {
            _animator = animator;
        }

        void IIkRig.OnPreProcess(float deltaTime)
        {
            var speed = deltaTime / TransitionTime;
            _weight = isActiveAndEnabled && IsWork ? _weight + speed : _weight - speed;
            _weight = Mathf.Clamp01(_weight);
        }
        
        void IIkRig.OnIkProcess(Vector3 offset)
        {
            var clamp = 1 - ClampAngle;

            _animator.SetLookAtPosition(Target.position + offset);
            _animator.SetLookAtWeight(_weight, BodyWeight, HeadWeight, EyeWeight, clamp );
        }
    }
}
