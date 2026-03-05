using Unity.SaveData.Core;

namespace Unity.SaveData
{
#if PACKAGE_VISUAL_SCRIPTING
    [Unity.VisualScripting.RenamedFrom("DataStore.IntContainer")]
#endif
    public class IntContainer : DataContainerBase<int> {   }
}