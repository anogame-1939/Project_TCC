using Unity.TinyCharacterController.Core;
#if PACKAGE_VISUAL_SCRIPTING
using Unity.VisualScripting;
#endif
using UnityEngine;
using Unity.TinyCharacterController.Interfaces.Core;

namespace Unity.TinyCharacterController.Interfaces.Components
{
    /// <summary>
    /// Interface for accessing the behavior results of the Brain
    /// </summary>
#if PACKAGE_VISUAL_SCRIPTING
    [RenamedFrom("TinyCharacterController.Interfaces.Components.IBrain")]
#endif
    public interface IBrain 
    {
        /// <summary>
        /// Velocity based on the character's direction
        /// </summary>
        Vector3 LocalVelocity { get; }
        
        /// <summary>
        /// Velocity of the currently active Control
        /// </summary>
#if PACKAGE_VISUAL_SCRIPTING
        [RenamedFrom("ControllableVelocity")]
#endif
        Vector3 ControlVelocity { get;  }
        
        /// <summary>
        /// Total Velocity of Effects
        /// </summary>
#if PACKAGE_VISUAL_SCRIPTING
        [RenamedFrom("AdditionalVelocity")]
#endif
        Vector3 EffectVelocity { get; }
        
        /// <summary>
        /// Final Velocity
        /// </summary>
        Vector3 TotalVelocity { get; }
        
        /// <summary>
        /// Move of the currently selected Control by the character
        /// </summary>
        IMove CurrentMove { get;  }
        
        /// <summary>
        /// Turn of the currently selected Control by the character
        /// </summary>
        ITurn CurrentTurn { get;  }
        
        /// <summary>
        /// Current character movement speed
        /// </summary>
#if PACKAGE_VISUAL_SCRIPTING
        [RenamedFrom("Speed")]
#endif
        float CurrentSpeed { get; }
        
        /// <summary>
        /// Speed at which the character's direction is updated. If -1, it is updated immediately.
        /// </summary>
        int TurnSpeed { get; }

        /// <summary>
        /// Character's orientation
        /// </summary>
        float YawAngle { get; }
        
        /// <summary>
        /// Difference between the character's orientation in the current frame and the previous frame
        /// </summary>
        float DeltaTurnAngle { get; }
    }

    /// <summary>
    /// Callback called when CharacterSettings values change.
    /// Mainly used for changing CharacterController or Collider sizes.
    /// </summary>
    public interface ICharacterSettingUpdateReceiver
    {
        /// <summary>
        /// CharacterSettings values have changed
        /// </summary>
        /// <param name="settings">Changed CharacterSettings</param>
        void OnUpdateSettings(CharacterSettings settings);
    }
}
