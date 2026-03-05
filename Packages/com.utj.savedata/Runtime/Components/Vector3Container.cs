using Unity.SaveData.Core;
#if PACKAGE_VISUAL_SCRIPTING
using Unity.VisualScripting;
#endif
using UnityEngine;

namespace Unity.SaveData
{
#if PACKAGE_VISUAL_SCRIPTING
    [RenamedFrom("DataStore.Vector3Container")]
#endif
    public class Vector3Container : DataContainerBase<Vector3> {   }
}