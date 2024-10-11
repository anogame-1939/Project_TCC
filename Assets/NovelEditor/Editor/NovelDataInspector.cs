using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using UnityEngine.UIElements;
using UnityEditorInternal;
using NovelEditor;

namespace NovelEditor.Editor
{
    /// <summary>
    /// NovelDataのインスペクター拡張
    /// </summary>
    [CustomEditor(typeof(NovelData))]
    internal class NovelDataInspector : UnityEditor.Editor
    {
        NovelData noveldata;
        ProgressBar bar;
        Label label;

        private ReorderableList reorderableList;

        void OnEnable()
        {
            noveldata = target as NovelData;

            LocationWrapper wrapper = new LocationWrapper() { locations = noveldata.locations };
            string json = JsonUtility.ToJson(wrapper);
            noveldata.newLocations = JsonUtility.FromJson<LocationWrapper>(json).locations;
            SetReorderableList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            reorderableList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        public override VisualElement CreateInspectorGUI()
        {
            var visualElement = new VisualElement();
            visualElement.styleSheets.Add(Resources.Load<StyleSheet>("NovelDataUSS"));

            var container = new IMGUIContainer(OnInspectorGUI);
            visualElement.Add(container);
            var visualTree = Resources.Load<VisualTreeAsset>("NovelDataUXML");
            visualTree.CloneTree(visualElement);

            var button = visualElement.Q<Button>("open_button");
            button.clickable.clicked += OpenEditor;

            var prefabButton = visualElement.Q<Button>("prefab_button");
            prefabButton.clickable.clicked += ChangePrefab;

            label = visualElement.Q<Label>("progressLabel");

            bar = visualElement.Q<ProgressBar>();
            bar.style.display = DisplayStyle.None;


            return visualElement;
        }

        void SetReorderableList()
        {
            reorderableList = new ReorderableList(this.serializedObject, this.serializedObject.FindProperty("newLocations"));
            reorderableList.drawElementCallback = (rect, index, active, focused) =>
            {
                EditorGUI.ObjectField(rect, this.serializedObject.FindProperty("newLocations").GetArrayElementAtIndex(index));
            };
            reorderableList.drawHeaderCallback = (rect) => EditorGUI.LabelField(rect, "立ち絵の位置");

        }

        void OpenEditor()
        {
            if (noveldata.newData)
            {
                noveldata.ResetData();
            }
            NovelEditor.Open(noveldata);
        }

        /// <summary>
        /// 立ち絵位置のプレハブを変更した時、同じプレハブなら設定を引き継ぐ処理
        /// </summary>
        void ChangePrefab()
        {
            if (noveldata.newData)
            {
                noveldata.ResetData();
            }

            label.text = "処理中";
            bar.value = 0;
            bar.style.display = DisplayStyle.Flex;


            //idとインデックスの辞書型
            Dictionary<int, int> locationsKey = new Dictionary<int, int>();

            for (int i = 0; i < noveldata.locations.Count; i++)
            {
                locationsKey.Add(noveldata.locations[i].GetInstanceID(), i);
            }

            //nullの項目を削除する
            noveldata.newLocations.RemoveAll(item => item == null);
            noveldata.newLocations = noveldata.newLocations.Distinct().ToList();
            noveldata.changeLocation(noveldata.newLocations);

            //プログレスバー用の数値
            float perParagraph = 100 / noveldata.paragraphList.Count;

            //全てのParagraphDataのDialogueの立ち絵情報を変更
            foreach (NovelData.ParagraphData pdata in noveldata.paragraphList)
            {
                //一つのParagraphを処理するたびにプログレスバーに加算される
                float perDialogue = perParagraph / pdata.dialogueList.Count;

                //オブジェクトプールにあるParagraphならスキップ
                if (!pdata.enabled)
                {
                    bar.value += perDialogue;
                    continue;
                }

                foreach (NovelData.ParagraphData.Dialogue dialogue in pdata.dialogueList)
                {
                    //前のデータを保存
                    NovelData.ParagraphData.Dialogue preData = JsonUtility.FromJson<NovelData.ParagraphData.Dialogue>(JsonUtility.ToJson(dialogue));

                    //現在の立ち絵でデータを初期化
                    dialogue.charas = new Sprite[noveldata.locations.Count];
                    dialogue.howCharas = new CharaChangeStyle[noveldata.locations.Count];
                    dialogue.charaFadeColor = new Color[noveldata.locations.Count];
                    dialogue.charaEffects = new Effect[noveldata.locations.Count];
                    dialogue.charaEffectStrength = new int[noveldata.locations.Count];

                    //同じIDのプレハブがあれば差し替え
                    for (int i = 0; i < noveldata.locations.Count; i++)
                    {
                        if (locationsKey.ContainsKey(noveldata.locations[i].GetInstanceID()))
                        {
                            int preIndex = locationsKey[noveldata.locations[i].GetInstanceID()];

                            dialogue.charas[i] = preData.charas[preIndex];
                            dialogue.howCharas[i] = preData.howCharas[preIndex];
                            dialogue.charaFadeColor[i] = preData.charaFadeColor[preIndex];
                            dialogue.charaEffects[i] = preData.charaEffects[preIndex];
                            dialogue.charaEffectStrength[i] = preData.charaEffectStrength[preIndex];
                        }
                    }

                    bar.value += perDialogue;
                }
            }

            bar.value = 100;
            label.text = "処理完了";

            EditorUtility.SetDirty(noveldata);
        }

        class LocationWrapper
        {
            public List<UnityEngine.UI.Image> locations;
        }
    }
}