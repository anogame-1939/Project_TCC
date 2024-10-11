using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace NovelEditor.Editor
{
    /// <summary>
    /// ヒエラルキーにて右クリックでUIを作成するためのクラス
    /// </summary>
    internal class CreateUI
    {
        [MenuItem("GameObject/UI/NovelUI", false, 10)]
        private static void CreateNovelUI(MenuCommand menuCommand)
        {
            // ゲームオブジェクトを生成します
            var novelUI = Resources.Load<GameObject>("NovelPlayer"); ;

            var obj = GameObject.Instantiate(novelUI);
            obj.name = "NovelPlayer";

            // 親を設定して同じレイヤーを継承
            GameObjectUtility.SetParentAndAlign(obj, menuCommand.context as GameObject);

            //クラッシュする...
            //Undo.RegisterCreatedObjectUndo(obj, "Create NovelUI");

            // 生成したゲームオブジェクトを選択状態に
            Selection.activeObject = obj;
        }

        [MenuItem("GameObject/UI/ChoiceButton", false, 10)]
        private static void CreateChoiceButton(MenuCommand menuCommand)
        {
            // ゲームオブジェクトを生成します
            var button = Resources.Load<GameObject>("ChoiceButton"); ;

            var obj = GameObject.Instantiate(button);
            obj.name = "ChoiceButton";

            // 親を設定して同じレイヤーを継承
            GameObjectUtility.SetParentAndAlign(obj, menuCommand.context as GameObject);

            //クラッシュする...
            //Undo.RegisterCreatedObjectUndo(obj, "Create Button");

            // 生成したゲームオブジェクトを選択状態に
            Selection.activeObject = obj;
        }
    }
}