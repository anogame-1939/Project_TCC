using UnityEngine;
using UnityEditor;

namespace AnoGame.Direction
{
    [CustomEditor(typeof(BigManDirection))]
    public class BigManDirectionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // デフォルトのInspector描画
            base.OnInspectorGUI();

            // 対象スクリプトの参照を取得
            BigManDirection bigManDirection = (BigManDirection)target;

            // ボタンを配置 (Play中のみ押せるように)
            if (UnityEngine.Application.isPlaying)
            {
                if (GUILayout.Button("FirstMove を実行"))
                {
                    // publicメソッドを呼び出してコルーチン開始
                    bigManDirection.StartFirstMove();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("FirstMoveを実行するにはPlayモードにしてください。", MessageType.Info);
            }
        }
    }
}
