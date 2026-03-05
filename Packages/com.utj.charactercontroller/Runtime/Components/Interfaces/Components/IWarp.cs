#if PACKAGE_VISUAL_SCRIPTING
using Unity.VisualScripting;
#endif
using UnityEngine;

namespace Unity.TinyCharacterController.Interfaces.Components
{
    /// <summary>
    /// Updates the character's position.
    /// When updating the position through warping, do not perform movement using Control or SetVelocity.
    /// </summary>
#if PACKAGE_VISUAL_SCRIPTING
    [RenamedFrom("TinyCharacterController.Interfaces.IWarp")]
#endif
#if PACKAGE_VISUAL_SCRIPTING
    [RenamedFrom("TinyCharacterController.Interfaces.Components.IWarp")]
#endif
    public interface IWarp 
    {
        /// <summary>
        /// Warps the target.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="direction"></param>
        void Warp(Vector3 position, Vector3 direction);

        /// <summary>
        /// Updates the target's position.
        /// Does not update the direction.
        /// </summary>
        /// <param name="position"></param>
#if PACKAGE_VISUAL_SCRIPTING
        [RenamedFrom("SetPosition")]
#endif
        void Warp(Vector3 position);

        /// <summary>
        /// Updates the target's rotation.
        /// Does not update the position.
        /// </summary>
        /// <param name="rotation"></param>
#if PACKAGE_VISUAL_SCRIPTING
        [RenamedFrom("SetRotation")]
#endif
        void Warp(Quaternion rotation);

        /// <summary>
        /// Updates the target's position.
        /// Considers obstacles instead of directly moving the coordinates.
        /// </summary>
        /// <param name="position"></param>
        void Move(Vector3 position);
    }
}