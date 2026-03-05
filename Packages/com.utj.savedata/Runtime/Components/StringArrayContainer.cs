using Unity.SaveData.Core;

namespace Unity.SaveData
{
#if PACKAGE_VISUAL_SCRIPTING
    [Unity.VisualScripting.RenamedFrom("DataStore.StringArrayContainer")]
#endif
    public class StringArrayContainer : DataContainerBase<string[]> {}
}

