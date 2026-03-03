#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using AnoGame.Data;
using AnoGame.Data.Editor; // ItemGeneratorWindowのデータ構造を利用

public class BatchItemCreator
{
    // JSONファイルのパス
    private const string JSON_PATH = "Assets/_Project/Data/ItemsResources/items_batch.json";
    // 保存先
    private const string SAVE_PATH = "Assets/_Project/Data/ItemsResources";
    // データベースのパス (プロジェクトに合わせて調整が必要。ここでは検索して取得)
    private const string DB_NAME = "ItemDatabase"; 

    [MenuItem("Tools/Run Batch Item Creation")]
    public static void RunBatch()
    {
        if (!File.Exists(JSON_PATH))
        {
            Debug.LogError($"JSON file not found at: {JSON_PATH}");
            return;
        }

        string jsonContent = File.ReadAllText(JSON_PATH);
        
        // ItemGeneratorWindowの内部クラスを使いたいが、private/protectedなら使えない。
        // ここでは単純化のため、同じ構造体定義を使ってデシリアライズする
        // (ItemGeneratorWindow.cs の ItemJsonData クラス定義が public であることを前提)
        
        // 配列でラップ
        string wrappedJson = "{\"items\":" + jsonContent + "}";
        ItemJsonList dataList = JsonUtility.FromJson<ItemJsonList>(wrappedJson);

        if (dataList == null || dataList.items == null)
        {
            Debug.LogError("Failed to parse JSON.");
            return;
        }

        // Databaseを探す
        string[] guids = AssetDatabase.FindAssets("t:ItemDatabase");
        if (guids.Length == 0)
        {
            Debug.LogError("ItemDatabase asset not found in project.");
            return;
        }
        string dbPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(dbPath);

        if (database == null)
        {
            Debug.LogError("Failed to load ItemDatabase.");
            return;
        }

        // フォルダ確認
        if (!Directory.Exists(SAVE_PATH)) Directory.CreateDirectory(SAVE_PATH);

        int count = 0;
        foreach (var data in dataList.items)
        {
            CreateItem(data, database);
            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Batch Creation Complete! Processed {count} items.");
    }

    private static void CreateItem(ItemJsonData data, ItemDatabase database)
    {
        string assetPath = $"{SAVE_PATH}/{data.id}.asset";
        
        ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
        bool isNew = false;
        
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<ItemData>();
            isNew = true;
        }

        // SerializedObjectで書き込み
        SerializedObject so = new SerializedObject(item);
        so.Update();

        SetProp(so, "itemId", data.id);
        SetProp(so, "itemName", data.displayName);
        SetProp(so, "description", data.description);

        SerializedProperty pItemType = so.FindProperty("itemType");
        if (pItemType != null && Enum.TryParse<ItemType>(data.type, true, out var typeEnum))
        {
            pItemType.enumValueIndex = (int)typeEnum;
        }

        so.ApplyModifiedProperties();

        if (isNew)
        {
            AssetDatabase.CreateAsset(item, assetPath);
        }
        else
        {
            EditorUtility.SetDirty(item);
        }

        // DB登録
        RegisterToDatabase(database, item);
    }

    private static void SetProp(SerializedObject so, string name, string value)
    {
        var p = so.FindProperty(name);
        if (p != null) p.stringValue = value;
    }

    private static void RegisterToDatabase(ItemDatabase database, ItemData item)
    {
        SerializedObject dbSO = new SerializedObject(database);
        dbSO.Update();
        SerializedProperty itemsProp = dbSO.FindProperty("items");
        
        bool exists = false;
        // 既存のリストを走査して、既に登録されていないかチェック
        for (int i = 0; i < itemsProp.arraySize; i++)
        {
            var prop = itemsProp.GetArrayElementAtIndex(i);
            if (prop.objectReferenceValue == item)
            {
                exists = true;
                break;
            }
        }

        // 存在しない場合のみ追加
        if (!exists)
        {
            int index = itemsProp.arraySize;
            itemsProp.arraySize++;
            itemsProp.GetArrayElementAtIndex(index).objectReferenceValue = item;
            dbSO.ApplyModifiedProperties();
            Debug.Log($"[Batch] Registered new item to DB: {item.name}");
        }
        else
        {
             Debug.Log($"[Batch] Item already in DB: {item.name}");
        }
    }
}
#endif
