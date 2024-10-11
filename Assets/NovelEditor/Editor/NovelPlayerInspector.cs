using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace NovelEditor.Editor
{
    /// <summary>
    /// NovelPlayerのインスペクター拡張
    /// </summary>
    [CustomEditor(typeof(NovelPlayer))]
    internal class NovelPlayerInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            try
            {
                serializedObject.Update();
                base.OnInspectorGUI();
                serializedObject.ApplyModifiedProperties();
            }
            catch { }

        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var label = new Label();
            label.text = "会話パートの設定はこのコンポーネントだけいじる";
            root.Add(label);

            var visualTree = Resources.Load<VisualTreeAsset>("NovelPlayerUXML");
            visualTree.CloneTree(root);

            var _novelData = root.Q<ObjectField>("_novelData");
            _novelData.BindProperty(serializedObject.FindProperty("_novelData"));

            var _choiceButton = root.Q<ObjectField>("_choiceButton");
            _choiceButton.BindProperty(serializedObject.FindProperty("_choiceButton"));

            var _dialogueSprite = root.Q<ObjectField>("_dialogueSprite");
            _dialogueSprite.BindProperty(serializedObject.FindProperty("_dialogueSprite"));

            var _nonameDialogueSprite = root.Q<ObjectField>("_nonameDialogueSprite");
            _nonameDialogueSprite.BindProperty(serializedObject.FindProperty("_nonameDialogueSprite"));

            var _playOnAwake = root.Q<Toggle>("_playOnAwake");
            _playOnAwake.BindProperty(serializedObject.FindProperty("_playOnAwake"));

            var _hideAfterPlay = root.Q<Toggle>("_hideAfterPlay");
            _hideAfterPlay.BindProperty(serializedObject.FindProperty("_hideAfterPlay"));

            var _hideFadeTime = root.Q<FloatField>("_hideFadeTime");
            _hideFadeTime.BindProperty(serializedObject.FindProperty("_hideFadeTime"));

            var _isDisplay = root.Q<Toggle>("_isDisplay");
            _isDisplay.BindProperty(serializedObject.FindProperty("_isDisplay"));

            var _charaFadeTime = root.Q<FloatField>("_charaFadeTime");
            _charaFadeTime.BindProperty(serializedObject.FindProperty("_charaFadeTime"));

            var _textSpeed = root.Q<SliderInt>("_textSpeed");
            _textSpeed.BindProperty(serializedObject.FindProperty("_textSpeed"));

            var _BGMVolume = root.Q<Slider>("_BGMVolume");
            _BGMVolume.BindProperty(serializedObject.FindProperty("_BGMVolume"));

            var _SEVolume = root.Q<Slider>("_SEVolume");
            _SEVolume.BindProperty(serializedObject.FindProperty("_SEVolume"));

            var inputEnum = root.Q<EnumField>("Input");
            inputEnum.Init((HowInput)serializedObject.FindProperty("_inputSystem").enumValueIndex);
            inputEnum.BindProperty(serializedObject.FindProperty("_inputSystem"));
            inputEnum.RegisterValueChangedCallback(x =>
            {
                if ((HowInput)serializedObject.FindProperty("_inputSystem").enumValueIndex == HowInput.UserSetting)
                {
                    root.Q<IMGUIContainer>().style.display = DisplayStyle.Flex;
                }
                else
                {
                    root.Q<IMGUIContainer>().style.display = DisplayStyle.None;
                }
            });

            var inputList = new IMGUIContainer(OnInspectorGUI);
            root.Add(inputList);

            return root;
        }
    }
}
