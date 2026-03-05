using Unity.SaveData.Core;

namespace Unity.SaveData
{
#if PACKAGE_VISUAL_SCRIPTING
    [Unity.VisualScripting.RenamedFrom("DataStore.StringContainer")]
#endif
    public class StringContainer : DataContainerBase<string> {   }
}