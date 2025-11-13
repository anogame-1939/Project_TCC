#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Playables;

[InitializeOnLoad]
public static class StickyTimelinePreview
{
    private const string MENU_PATH = "Tools/Sticky Preview";

    private static bool _enabled;
    private static PlayableDirector _director;
    private static DirectorUpdateMode _originalUpdateMode;
    private static double _startDirectorTime;
    private static double _startEditorTime;

    static StickyTimelinePreview()
    {
        EditorApplication.update += OnEditorUpdate;
        Selection.selectionChanged += OnSelectionChanged;
    }

    // ▼ トグル本体
    [MenuItem(MENU_PATH)]
    private static void Toggle()
    {
        _enabled = !_enabled;
        Menu.SetChecked(MENU_PATH, _enabled);

        if (!_enabled)
        {
            StopPreview();
        }
        else
        {
            TrySetDirectorFromSelection();
        }
    }

    // ▼ メニューのチェックマーク維持
    [MenuItem(MENU_PATH, true)]
    private static bool ToggleValidate()
    {
        Menu.SetChecked(MENU_PATH, _enabled);
        return true;
    }

    // ▼ Hierarchy の選択が変わったとき
    private static void OnSelectionChanged()
    {
        if (!_enabled) return;
        TrySetDirectorFromSelection();
    }

    // ▼ 現在の選択から Director を拾う共通処理
    private static void TrySetDirectorFromSelection()
    {
        var go = Selection.activeGameObject;
        if (go == null) return;

        // 選択オブジェクト or 親にある PlayableDirector を拾う
        var dir = go.GetComponentInParent<PlayableDirector>();
        if (dir != null)
        {
            SetDirector(dir);
        }
        // PlayableDirector が無い場合は、今の _director を維持（何もしない）
    }

    // ▼ 再生対象の Director をセット
    private static void SetDirector(PlayableDirector director)
    {
        _director = director;

        if (_director == null || _director.playableAsset == null)
            return;

        // Timeline Window をこの Director に固定
        ForceTimelineWindowToUse(_director);
    }

    // ▼ プレビュー停止＆後片付け
    private static void StopPreview()
    {
        if (_director != null)
        {
            try
            {
                _director.timeUpdateMode = _originalUpdateMode;
                _director.Stop();
            }
            catch
            {
                // オブジェクト削除済みなどの場合は握りつぶし
            }
        }

        _director = null;
    }

    // ▼ エディタのフレーム毎に呼ばれる
    private static void OnEditorUpdate()
    {
        if (!_enabled) return;
        if (_director == null) return;
        if (Application.isPlaying) return; // 再生中は何もしない

        var elapsed = EditorApplication.timeSinceStartup - _startEditorTime;
        var newTime = _startDirectorTime + elapsed;

        var duration = _director.duration;

        if (duration > 0.0)
        {
            switch (_director.extrapolationMode)
            {
                case DirectorWrapMode.Loop:
                    newTime %= duration;
                    break;

                case DirectorWrapMode.Hold:
                    newTime = Math.Min(newTime, duration);
                    break;

                case DirectorWrapMode.None:
                    if (newTime > duration)
                    {
                        newTime = duration;
                        // 一周で停止したいならここでトグルOFFにする
                        _enabled = false;
                        Menu.SetChecked(MENU_PATH, false);
                    }
                    break;
            }
        }

        _director.time = newTime;
        _director.DeferredEvaluate(); // エディタ上でプレビュー更新
    }

    private static void ForceTimelineWindowToUse(PlayableDirector director)
    {
        TimelineReflectionUtility.ForceSetDirector(director);
    }



}
#endif
