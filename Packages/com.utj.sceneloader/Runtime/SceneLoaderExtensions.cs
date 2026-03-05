#if PACKAGE_VISUAL_SCRIPTING
using Unity.VisualScripting;
#endif
using UnityEngine;

namespace Unity.SceneManagement
{
#if PACKAGE_VISUAL_SCRIPTING
    [RenamedFrom("Tcc.SceneManagement.SceneLoaderExtensions")]
#endif
    public static class SceneLoaderExtensions 
    {
        /// <summary>
        /// Get the owner of a scene added by SceneLoader.
        /// </summary>
        /// <param name="obj">The GameObject.</param>
        /// <returns>The owner's GameObject.</returns>
        public static GameObject GetOwner(this GameObject obj)
        {
            var scene = obj.scene;
            return SceneLoaderManager.GetOwner(scene);
        }
    }

}
