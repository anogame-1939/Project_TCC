using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using AnoGame.Data;

namespace AnoGame.Data.Editor
{
    // --- AIからの入力JSONデータ構造 ---
    [Serializable]
    public class ItemJsonData
    {
        public string id;           // 例: "Item_Magnet"
        public string displayName;  // 例: "Ｕ字磁石"
        public string description;  // 例: "理科の実験で使うような磁石..."
        public string type;         // 例: "Tool" (Enum変換はプロジェクトに合わせて)
        public string iconName;     // 例: "Icon_Magnet"
    }

    [Serializable]
    public class ItemJsonList
    {
        public List<ItemJsonData> items;
    }

    // --- ツール本体 ---
    public class ItemGeneratorWindow : EditorWindow
    {
        string jsonInput = "";

        // 【重要】あなたのプロジェクトのItemDatabaseクラスを指定してください
        ItemDatabase targetDatabase;

        // 保存先フォルダ
        string savePath = "Assets/_Project/Data/ItemsResources";

        [MenuItem("Tools/SLFB Item Generator")]
        public static void ShowWindow()
        {
            GetWindow<ItemGeneratorWindow>("Item Generator");
        }

        void OnGUI()
        {
            GUILayout.Label("Step 1: Item Data Generator", EditorStyles.boldLabel);

            // データベースのセット（手動でD&D）
            targetDatabase = (ItemDatabase)EditorGUILayout.ObjectField("Target DB", targetDatabase, typeof(ItemDatabase), false);
            savePath = EditorGUILayout.TextField("Save Path", savePath);

            GUILayout.Space(10);
            GUILayout.Label("Paste JSON List Here:", EditorStyles.miniLabel);
            jsonInput = EditorGUILayout.TextArea(jsonInput, GUILayout.Height(150));

            if (GUILayout.Button("Generate & Register Items", GUILayout.Height(40)))
            {
                GenerateItems();
            }
        }

        void GenerateItems()
        {
            if (targetDatabase == null)
            {
                Debug.LogError("Error: 登録先のItemDatabaseをセットしてください！");
                return;
            }

            if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);

            try
            {
                // JSON配列の整形（ルート要素がないとJsonUtilityが読めないためラップする）
                string fixedJson = jsonInput.Trim();
                if (fixedJson.StartsWith("[")) fixedJson = "{\"items\":" + fixedJson + "}";

                ItemJsonList dataList = JsonUtility.FromJson<ItemJsonList>(fixedJson);

                if (dataList != null && dataList.items != null)
                {
                    foreach (var data in dataList.items)
                    {
                        CreateAndRegisterItem(data);
                    }

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    Debug.Log($"Success: {dataList.items.Count} 個のアイテムを処理しました。");
                }
                else
                {
                    Debug.LogError("Error: JSON Parsing failed or empty list.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("JSON Parse Error: " + e.Message);
            }
        }

        void CreateAndRegisterItem(ItemJsonData data)
        {
            string assetPath = $"{savePath}/{data.id}.asset";

            // 既存アセットがあればロード、なければ新規作成
            // 【重要】ItemDataクラス名はプロジェクトに合わせて変更してください
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
            bool isNew = false;
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemData>();
                isNew = true;
            }

            // --- データの流し込み (SerializedObjectを使用) ---
            SerializedObject so = new SerializedObject(item);
            so.Update();

            SerializedProperty pItemId = so.FindProperty("itemId");
            if (pItemId != null) pItemId.stringValue = data.id;

            SerializedProperty pItemName = so.FindProperty("itemName");
            if (pItemName != null) pItemName.stringValue = data.displayName;

            SerializedProperty pDescription = so.FindProperty("description");
            if (pDescription != null) pDescription.stringValue = data.description;

            // Enumのパース
            SerializedProperty pItemType = so.FindProperty("itemType");
            if (pItemType != null && !string.IsNullOrEmpty(data.type))
            {
                if (Enum.TryParse<ItemType>(data.type, true, out ItemType parsedType))
                {
                    pItemType.enumValueIndex = (int)parsedType;
                }
                else
                {
                    // デフォルトなどを設定、あるいは警告
                    Debug.LogWarning($"ItemType '{data.type}' could not be parsed. defaulting.");
                }
            }

            so.ApplyModifiedProperties();

            // アイコン自動設定（Resourcesフォルダにある場合）
            // item.icon = Resources.Load<Sprite>("Icons/" + data.iconName);

            if (isNew)
            {
                AssetDatabase.CreateAsset(item, assetPath);
            }
            else
            {
                EditorUtility.SetDirty(item);
            }

            // --- データベースへの登録 ---
            // List<ItemData> items is private, so we need Key access via SerializedObject of Database
            RegisterToDatabase(item);
        }

        void RegisterToDatabase(ItemData item)
        {
            SerializedObject dbInfo = new SerializedObject(targetDatabase);
            dbInfo.Update();

            SerializedProperty itemsProp = dbInfo.FindProperty("items");
            if (itemsProp == null) return;

            bool contains = false;
            for (int i = 0; i < itemsProp.arraySize; i++)
            {
                SerializedProperty elem = itemsProp.GetArrayElementAtIndex(i);
                if (elem.objectReferenceValue == item)
                {
                    contains = true;
                    break;
                }
            }

            if (!contains)
            {
                itemsProp.arraySize++;
                SerializedProperty newElem = itemsProp.GetArrayElementAtIndex(itemsProp.arraySize - 1);
                newElem.objectReferenceValue = item;
                dbInfo.ApplyModifiedProperties();
                Debug.Log($"Registered {item.name} to Database.");
            }
        }
    }
}
