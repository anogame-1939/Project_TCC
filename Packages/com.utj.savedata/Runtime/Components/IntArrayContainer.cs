using Unity.SaveData.Core;

namespace Unity.SaveData
{
#if PACKAGE_VISUAL_SCRIPTING
    [Unity.VisualScripting.RenamedFrom("DataStore.IntArrayContainer")]
#endif
    public class IntArrayContainer : DataContainerBase<int[]> {}
}

