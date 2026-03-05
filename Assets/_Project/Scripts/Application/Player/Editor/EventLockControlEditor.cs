    #if UNITY_EDITOR
    using UnityEditor;
    using UnityEngine;

    namespace AnoGame.Application.Player.Control.Editor
    {
        [CustomEditor(typeof(EventLockControl))]
        public class EventLockControlEditor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                // 1) 既定の Inspector
                DrawDefaultInspector();

                // 2) SerializedProperty の更新開始
                serializedObject.Update();

                // 3) 対象プロパティを取得
                var spFollow      = serializedObject.FindProperty("followTarget");
                var spLookAt      = serializedObject.FindProperty("lookAtTarget");
                var spTargetPoint = serializedObject.FindProperty("targetPoint");

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("=== Test Buttons ===", EditorStyles.boldLabel);

                var c = (EventLockControl)target;

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Begin Lock")) c.BeginLock();
                    if (GUILayout.Button("End Lock"))   c.EndLock();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Freeze"))     c.Freeze();
                    if (GUILayout.Button("Face Keep"))  c.LookKeep();
                    if (GUILayout.Button("Face Move"))  c.LookFaceMove();
                }

                EditorGUILayout.Space();
                if (GUILayout.Button("Move Constant (Forward x 2 m/s)"))
                    c.MoveConstant(c.transform.forward, 2f);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Move To / Follow Helpers", EditorStyles.miniBoldLabel);

                // 4) ここで実体のフィールドを描画（消えない）
                
                EditorGUILayout.PropertyField(spTargetPoint, new GUIContent("Target Point"));
                EditorGUILayout.PropertyField(spFollow,      new GUIContent("Follow Target"));
                EditorGUILayout.PropertyField(spLookAt,      new GUIContent("Look At Target"));

                // 5) 変更を確定（以降は最新値を使える）
                serializedObject.ApplyModifiedProperties();


                // 6) ボタン（向く & 移動をセットで実行）
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Set & MoveTo (face + go)"))
                    {
                        c.BeginLock();                 // 必要なければ外してOK
                        c.LookFaceMove();              // まず進行方向へ向く

                        // ★ 目的地の決定：FollowTarget があればその“今の位置”、なければ TargetPoint の値
                        var follow = (Transform)serializedObject.FindProperty("followTarget").objectReferenceValue;
                        var dest   = (follow != null)
                                    ? follow.position
                                    : serializedObject.FindProperty("targetPoint").vector3Value;

                        c.MoveToPoint(dest, 2f);       // その座標へ MoveTo
                        EditorUtility.SetDirty(c);
                    }

                    if (GUILayout.Button("Set & Follow (face + go)"))
                    {
                        c.BeginLock();
                        c.LookFaceMove();
                        var follow = (Transform)serializedObject.FindProperty("followTarget").objectReferenceValue;
                        c.Follow(follow, 2f);
                        EditorUtility.SetDirty(c);
                    }
                }

                // 単独の LookAt はそのままでもOK
                if (GUILayout.Button("LookAt (Look At Target)"))
                {
                    var lookAt = (Transform)serializedObject.FindProperty("lookAtTarget").objectReferenceValue;
                    c.LookAt(lookAt);
                }
            }
        }
    }
    #endif
