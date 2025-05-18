#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using AnoGame.Application.Enemy;

namespace AnoGame.Editor
{
    [CustomEditor(typeof(TreeFeller))]
    public class TreeFellerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // 既存のインスペクター描画
            DrawDefaultInspector();

            // 対象となるスクリプトの参照を取得
            TreeFeller treeFeller = (TreeFeller)target;

            // ボタンを描画し、押された場合にFellTrees()を実行
            if (GUILayout.Button("Fell Trees"))
            {
                treeFeller.FellTrees();
            }
        }
    }
    #endif

}
