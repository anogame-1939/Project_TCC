using UnityEngine;
using UnityEditor;
using System.Text;

namespace AnoGame.Data.Editor
{
    [CustomEditor(typeof(ItemDatabase))]
    public class ItemDatabaseEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var db = (ItemDatabase)target;

            if (GUILayout.Button("TSV形式でコピー"))
            {
                var sb = new StringBuilder();
                foreach (var item in db.Items)
                {
                    if (item == null) continue;
                    // 1列目: ItemName, 2列目: ItemName + ".desc"
                    sb.Append(item.ItemName)
                      .Append('\t')
                      .Append(item.ItemName)
                      .AppendLine()
                      .Append(item.ItemName)
                      .Append(".desc")
                      .Append('\t')
                      .Append(item.Description)
                      .AppendLine();
                }

                var tsv = sb.ToString().TrimEnd('\r','\n');
                // クリップボードにコピー
                EditorGUIUtility.systemCopyBuffer = tsv;
                Debug.Log($"[ItemDatabase] {db.Items.Count} 件の TSV をクリップボードにコピーしました。\n{tsv}");
            }
        }
    }
}