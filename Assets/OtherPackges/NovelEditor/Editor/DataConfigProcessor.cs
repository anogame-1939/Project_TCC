using UnityEditor;
using NovelEditor;

namespace NovelEditor.Editor
{
    /// <summary>
    /// アセットの削除時に編集中のデータが削除されたかをチェックするクラス
    /// </summary>
    internal class DataConfigProcessor : AssetModificationProcessor
    {
        private static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
        {
            NovelData data = AssetDatabase.LoadAssetAtPath<NovelData>(assetPath);
            if (data != null)
            {
                if (NovelEditorWindow.editingData == data)
                {
                    NovelEditorWindow.Instance.Init(null);
                }
            }
            return AssetDeleteResult.DidNotDelete;
        }

    }
}