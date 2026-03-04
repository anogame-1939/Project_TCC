using System.Reflection;
using UnityEditor.Timeline;
using UnityEngine.Playables;

public static class TimelineReflectionUtility
{
    public static void ForceSetDirector(PlayableDirector director)
    {
        // TimelineWindow を取得
        var window = TimelineEditor.GetWindow();
        if (window == null) return;

        // TimelineWindow の "state" を取得
        var type = window.GetType();
        var stateField = type.GetField("m_State", BindingFlags.NonPublic | BindingFlags.Instance);
        if (stateField == null) return;

        var state = stateField.GetValue(window);
        if (state == null) return;

        // state.masterSequence を取得
        var stateType = state.GetType();
        var masterSeqProperty = stateType.GetProperty("masterSequence", BindingFlags.Public | BindingFlags.Instance);
        var masterSeq = masterSeqProperty.GetValue(state);

        if (masterSeq == null) return;

        // masterSequence.director を書き換え
        var masterSeqType = masterSeq.GetType();
        var directorField = masterSeqType.GetField("m_Director", BindingFlags.NonPublic | BindingFlags.Instance);

        directorField.SetValue(masterSeq, director);

        // 再描画
        window.Repaint();
    }
}
