using UnityEngine;
using UnityEditor;
using UnityEngine.Playables;

namespace AnoGame.Editor
{
    [CustomEditor(typeof(PlayableDirector))]
    public class PlayableDirectorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // 元のインスペクタ表示
            DrawDefaultInspector();

            // 対象のPlayableDirectorを取得
            PlayableDirector director = (PlayableDirector)target;

            // 「Play Timeline」ボタンを追加
            if (GUILayout.Button("Play Timeline"))
            {
                if (director != null)
                {
                    director.Play();
                }
            }
        }
    }
}