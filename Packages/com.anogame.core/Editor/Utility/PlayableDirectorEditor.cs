using UnityEngine;
using UnityEditor;
using UnityEngine.Playables;
using System.Collections.Generic;
using System.Linq;

namespace AnoGame.Utility.Editor
{
    /// <summary>
    /// PlayableDirector のカスタムインスペクター。
    /// PlayableAsset を .playable ファイルのみに絞り込むドロップダウンを提供する。
    /// アタッチ時の InitialState デフォルトを Paused にする。
    /// </summary>
    [CustomEditor(typeof(PlayableDirector))]
    public class PlayableDirectorEditor : UnityEditor.Editor
    {
        private string[] _assetPaths;
        private string[] _assetNames;
        private PlayableAsset[] _assets;
        private bool _cacheBuilt;

        void OnEnable()
        {
            RebuildCache();
        }

        void RebuildCache()
        {
            var guids = AssetDatabase.FindAssets("t:PlayableAsset");
            var filtered = new List<(string path, string name, PlayableAsset asset)>();

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".playable"))
                    continue;

                var asset = AssetDatabase.LoadAssetAtPath<PlayableAsset>(path);
                if (asset != null)
                {
                    // フォルダ構造を含めた表示名を生成
                    var displayName = path
                        .Replace("Assets/", "")
                        .Replace(".playable", "");
                    filtered.Add((path, displayName, asset));
                }
            }

            // パス順でソート
            filtered.Sort((a, b) => string.Compare(a.path, b.path, System.StringComparison.Ordinal));

            _assetPaths = filtered.Select(f => f.path).ToArray();
            _assetNames = filtered.Select(f => f.name).ToArray();
            _assets = filtered.Select(f => f.asset).ToArray();
            _cacheBuilt = true;
        }

        public override void OnInspectorGUI()
        {
            var director = (PlayableDirector)target;

            // ── PlayableAsset ドロップダウン ──
            if (!_cacheBuilt)
                RebuildCache();

            EditorGUILayout.LabelField("Playable Asset (.playable)", EditorStyles.boldLabel);

            int currentIndex = -1;
            if (director.playableAsset != null)
            {
                for (int i = 0; i < _assets.Length; i++)
                {
                    if (_assets[i] == director.playableAsset)
                    {
                        currentIndex = i;
                        break;
                    }
                }
            }

            // ドロップダウン用のリストを構築 ("-- None --" + 各アセット名)
            var displayOptions = new string[_assetNames.Length + 1];
            displayOptions[0] = "-- None --";
            for (int i = 0; i < _assetNames.Length; i++)
                displayOptions[i + 1] = _assetNames[i];

            int dropdownIndex = currentIndex + 1; // 0 = None, 1~ = assets
            int newDropdownIndex = EditorGUILayout.Popup("Asset", dropdownIndex, displayOptions);

            if (newDropdownIndex != dropdownIndex)
            {
                Undo.RecordObject(director, "Change PlayableAsset");

                PlayableAsset previousAsset = director.playableAsset;

                if (newDropdownIndex == 0)
                {
                    director.playableAsset = null;
                }
                else
                {
                    director.playableAsset = _assets[newDropdownIndex - 1];
                }

                // アタッチ時（null → 非null）に InitialState を Paused に設定
                if (previousAsset == null && director.playableAsset != null)
                {
                    var so = new SerializedObject(director);
                    var initialStateProp = so.FindProperty("m_InitialState");
                    if (initialStateProp != null)
                    {
                        initialStateProp.intValue = 1; // 0 = Playing, 1 = Paused
                        so.ApplyModifiedProperties();
                    }
                }

                EditorUtility.SetDirty(director);
            }

            // キャッシュ更新ボタン
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh List", GUILayout.Width(100)))
            {
                RebuildCache();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // ── Default Inspector ──
            DrawDefaultInspector();

            EditorGUILayout.Space(4);

            // ── Play Timeline ボタン ──
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
