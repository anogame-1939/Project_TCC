using System;
using Unity.TinyCharacterController.Interfaces.Core;
using Unity.TinyCharacterController.Utility;
#if PACKAGE_VISUAL_SCRIPTING
using Unity.VisualScripting;
#endif
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.TinyCharacterController.Control
{
    /// <summary>
    /// キャラクターの向きを手動で設定するコンポーネント
    /// ManualControl推奨
    /// </summary>
    [Obsolete("Recommends Manual Control")]
    [AddComponentMenu(MenuList.MenuControl + nameof(ManualTurn))]
    [RequireComponent(typeof(CharacterSettings))]
#if PACKAGE_VISUAL_SCRIPTING
    [RenamedFrom("TinyCharacterController.ManualCharacterTurn")]
#endif
#if PACKAGE_VISUAL_SCRIPTING
    [RenamedFrom("TinyCharacterController.Control.ManualTurn")]
#endif
    [DisallowMultipleComponent]
    public class ManualTurn : MonoBehaviour, ITurn
    {
        [SerializeField]
        private Vector3 _direction = Vector3.forward;

        [FormerlySerializedAs("_turnPriority")]
        [Space]
        [Header("キャラクターの向きの設定")]
        [SerializeField]
        public int TurnPriority;

        [ Range(-1, 50)]
        [FormerlySerializedAs("_turnSpeed")]
        public int TurnSpeed = 30;

        int IPriority<ITurn>.Priority => (_direction == Vector3.zero) ? 0 : TurnPriority;
        int ITurn.TurnSpeed => TurnSpeed;

        public void SetRotation(Quaternion rotation)
        {
            _direction = rotation * Vector3.forward;
            _direction.y = 0;
        }

        public void SetDirection(Vector3 dir)
        {
            _direction = dir;
            _direction.y = 0;
        }

        float ITurn.YawAngle => Vector3.SignedAngle(Vector3.forward, _direction, Vector3.up);
    }
}
