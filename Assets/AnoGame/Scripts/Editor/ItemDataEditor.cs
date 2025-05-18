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
        var itemData = serializedObject.targetObject as ItemData;
        var number = GetDocumentNumber();
        var nameProperty = serializedObject.FindProperty("itemName");
        var typeProperty = serializedObject.FindProperty("itemType");
        var stackableProperty = serializedObject.FindProperty("isStackable");
        
        serializedObject.Update();
        
        nameProperty.stringValue = $"古い手記（{number}）";
        typeProperty.enumValueIndex = (int)ItemType.Consumable;
        stackableProperty.boolValue = false;
        
        serializedObject.ApplyModifiedProperties();
        
        // アセットのパスを取得
        string currentPath = AssetDatabase.GetAssetPath(itemData);
        // 新しいファイル名を生成（formatを変更）
        string newFileName = $"ItemData.501.Memo_{number}";
        string directory = System.IO.Path.GetDirectoryName(currentPath);
        string extension = System.IO.Path.GetExtension(currentPath);
        string newPath = System.IO.Path.Combine(directory, newFileName + extension);

        Debug.Log($"Renaming asset from {currentPath} to {newPath}");

        // アセットの名前を変更
        var result = AssetDatabase.MoveAsset(currentPath, newPath);
        if (!string.IsNullOrEmpty(result))
        {
            Debug.LogError($"Error renaming asset: {result}");
        }
        else
        {
            EditorUtility.SetDirty(itemData);
            AssetDatabase.SaveAssets();
        }
    }

    private int GetDocumentNumber()
    {
        var currentItem = target as ItemData;
        
        // 完全な型名を指定してアセットを検索
        var guids = AssetDatabase.FindAssets("t:AnoGame.Data.ItemData");
        Debug.Log($"Found {guids.Length} ItemData assets");

        // 古い手記のアイテムリストを取得
        var documentItems = guids
            .Select(guid =>
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                Debug.Log($"Found ItemData at path: {path}");
                return AssetDatabase.LoadAssetAtPath<ItemData>(path);
            })
            .Where(item =>
            {
                if (item == null)
                {
                    Debug.LogWarning("Loaded ItemData is null");
                    return false;
                }
                var isDocument = item.ItemName.StartsWith("古い手記");
                Debug.Log($"ItemData: {item.ItemName}, IsDocument: {isDocument}");
                return isDocument;
            })
            .OrderBy(item => AssetDatabase.GetAssetPath(item)) // パスでソート
            .ToList();

        // 現在のアイテムのインデックスを検索
        int index = documentItems.IndexOf(currentItem);
        int documentNumber = index >= 0 ? index + 1 : documentItems.Count + 1;

        Debug.Log($"Current index: {index}, documentNumber: {documentNumber}");

        return documentNumber;
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