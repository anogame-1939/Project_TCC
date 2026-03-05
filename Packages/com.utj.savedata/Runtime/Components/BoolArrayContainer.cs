using Unity.SaveData.Core;

namespace Unity.SaveData
{
#if PACKAGE_VISUAL_SCRIPTING
    [Unity.VisualScripting.RenamedFrom("DataStore.BoolArrayContainer")]
#endif
    public class BoolArrayContainer : DataContainerBase<bool[]> {}
}

