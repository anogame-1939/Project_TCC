using Unity.TinyCharacterController.Interfaces.Components;
using Unity.TinyCharacterController.Utility;
#if PACKAGE_VISUAL_SCRIPTING
using Unity.VisualScripting;
#endif
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.TinyCharacterController.Ik
{
    [AddComponentMenu(MenuList.Ik + "LimbRig")]
#if PACKAGE_VISUAL_SCRIPTING
    [RenamedFrom("TinyCharacterController.LimbRig")]
#endif
#if PACKAGE_VISUAL_SCRIPTING
    [RenamedFrom("TinyCharacterController.Ik.LimbRig")]
#endif
    public class LimbRig : MonoBehaviour, IIkRig
    {
        [FormerlySerializedAs("isWorking")] 
#if PACKAGE_VISUAL_SCRIPTING
        [RenamedFrom("isWorking")]
#endif
        public bool IsWorking = true;

        [SerializeField] private AvatarIKGoal _ikGoal;
        [SerializeField] private AvatarIKHint _ikHint;
        [SerializeField] private  Transform _target;
        [SerializeField] private  Transform _hint;
        
        [FormerlySerializedAs("transitionToEnable")] 
#if PACKAGE_VISUAL_SCRIPTING
        [RenamedFrom("transitionToEnable")]
#endif
        public float TransitionToEnable;
        
        [FormerlySerializedAs("transitionToDisable")] 
#if PACKAGE_VISUAL_SCRIPTING
        [RenamedFrom("transitionToDisable")]
#endif
        public float TransitionToDisable = 0.3f;
        
        private float _weight;
        private Animator _animator;

        void IIkRig.OnIkProcess(Vector3 offset)
        {
            _animator.SetIKPosition(_ikGoal, _target.position + offset);
            _animator.SetIKRotation(_ikGoal, _target.rotation);
            _animator.SetIKHintPosition(_ikHint, _hint.position + offset);
            
            _animator.SetIKHintPositionWeight(_ikHint, _weight);
            _animator.SetIKPositionWeight(_ikGoal, _weight);
            _animator.SetIKRotationWeight(_ikGoal, _weight);
        }

        bool IIkRig.IsValid => _target != null && _hint != null ;

        float IIkRig.Weight => _weight;

        void IIkRig.Initialize(Animator animator)
        {
            _animator = animator;
        }

        void IIkRig.OnPreProcess(float deltaTime)
        {
            _weight = isActiveAndEnabled && IsWorking ? 
                _weight + deltaTime / TransitionToEnable : 
                _weight - deltaTime / TransitionToDisable;
            _weight = Mathf.Clamp01(_weight);
        }
    }
}
