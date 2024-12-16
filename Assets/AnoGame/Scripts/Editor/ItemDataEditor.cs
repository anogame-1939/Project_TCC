#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using AnoGame.Data;

// 新しいScriptableObjectを作成
[CreateAssetMenu(fileName = "DocumentTemplates", menuName = "AnoGame/Editor/Document Templates")]
public class DocumentTemplateSettings : ScriptableObject
{
    [SerializeField]
    private List<string> templates = new List<string>
    {
        "「明治初年、寺院廃絶の折、諸仏具を神器として保管すべし―」",
        "「この山には古くから言い伝えが―」",
        "「かつて此の地にあった寺の僧は―」",
    };

    public List<string> Templates => templates;
}

// エディタ拡張を修正
[CustomEditor(typeof(ItemData))]
public class ItemDataEditor : Editor
{
    private const string DOCUMENT_PREFIX = "誰かの手記のようだ。言葉は古いがかろうじて読める。";
    private const string TEMPLATE_ASSET_PATH = "Assets/AnoGame/Editor/DocumentTemplates.asset";
    private bool _showTemplates = false;
    private DocumentTemplateSettings _templateSettings;

    private void OnEnable()
    {
        LoadOrCreateTemplateSettings();
    }

    private void LoadOrCreateTemplateSettings()
    {
        _templateSettings = AssetDatabase.LoadAssetAtPath<DocumentTemplateSettings>(TEMPLATE_ASSET_PATH);
        if (_templateSettings == null)
        {
            // テンプレート設定アセットが存在しない場合は作成
            _templateSettings = CreateInstance<DocumentTemplateSettings>();
            
            // フォルダが存在しない場合は作成
            var folderPath = System.IO.Path.GetDirectoryName(TEMPLATE_ASSET_PATH);
            if (!System.IO.Directory.Exists(folderPath))
            {
                System.IO.Directory.CreateDirectory(folderPath);
            }
            
            AssetDatabase.CreateAsset(_templateSettings, TEMPLATE_ASSET_PATH);
            AssetDatabase.SaveAssets();
        }
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        var itemData = (ItemData)target;
        var serializedObject = new SerializedObject(itemData);
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("古い手記作成支援", EditorStyles.boldLabel);
        
        if (GUILayout.Button("古い手記として作成"))
        {
            CreateAsAncientDocument(serializedObject);
        }

        EditorGUILayout.Space(5);
        _showTemplates = EditorGUILayout.Foldout(_showTemplates, "テンプレートテキスト");
        if (_showTemplates && _templateSettings != null)
        {
            EditorGUI.indentLevel++;
            
            // テンプレート設定の編集ボタン
            if (GUILayout.Button("テンプレートを編集"))
            {
                Selection.activeObject = _templateSettings;
            }

            foreach (var template in _templateSettings.Templates)
            {
                if (GUILayout.Button($"テンプレート: {template.Substring(0, Mathf.Min(30, template.Length))}..."))
                {
                    ApplyTemplate(serializedObject, template);
                }
            }
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void CreateAsAncientDocument(SerializedObject serializedObject)
    {
        var number = GetNextDocumentNumber();
        var nameProperty = serializedObject.FindProperty("itemName");
        var typeProperty = serializedObject.FindProperty("itemType");
        var stackableProperty = serializedObject.FindProperty("isStackable");
        
        serializedObject.Update();
        
        nameProperty.stringValue = $"古い手記（{number}）";
        typeProperty.enumValueIndex = (int)ItemType.Quest;
        stackableProperty.boolValue = false;
        
        serializedObject.ApplyModifiedProperties();
        
        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssets();
    }

    private int GetNextDocumentNumber()
    {
        // プロジェクト内のすべてのItemDataアセットを検索
        var guids = AssetDatabase.FindAssets("t:ItemData");
        var existingNumbers = guids
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .Select(path => AssetDatabase.LoadAssetAtPath<ItemData>(path))
            .Where(item => item != null && item.ItemName.StartsWith("古い手記"))
            .Select(item =>
            {
                // "古い手記（X）"から数字を抽出
                var name = item.ItemName;
                var startIndex = name.IndexOf('（') + 1;
                var endIndex = name.IndexOf('）');
                if (startIndex > 0 && endIndex > startIndex)
                {
                    if (int.TryParse(name.Substring(startIndex, endIndex - startIndex), out int number))
                    {
                        return number;
                    }
                }
                return 0;
            })
            .Where(num => num > 0)
            .ToList();

        // 既存の番号が無ければ1を返す
        if (!existingNumbers.Any())
        {
            return 1;
        }

        // 最大の番号 + 1 を返す
        return existingNumbers.Max() + 1;
    }

    private void ApplyTemplate(SerializedObject serializedObject, string templateText)
    {
        var descriptionProperty = serializedObject.FindProperty("description");
        
        serializedObject.Update();
        
        descriptionProperty.stringValue = $"{DOCUMENT_PREFIX} {templateText}";
        
        serializedObject.ApplyModifiedProperties();
        
        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssets();
    }
}
#endif