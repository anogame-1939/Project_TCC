using Unity.SaveData.Core;

namespace Unity.SaveData
{
#if PACKAGE_VISUAL_SCRIPTING
    [Unity.VisualScripting.RenamedFrom("DataStore.FloatArrayContainer")]
#endif
    public class FloatArrayContainer : DataContainerBase<float[]> {}
}

