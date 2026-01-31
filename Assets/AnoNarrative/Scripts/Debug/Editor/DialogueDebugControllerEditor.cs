using UnityEngine;
using UnityEditor;
using AnoGame.AnoNarrative.Debug;

[CustomEditor(typeof(DialogueDebugController))]
public class DialogueDebugControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        DialogueDebugController controller = (DialogueDebugController)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug Controls", EditorStyles.boldLabel);

        if (GUILayout.Button("Play Target ID"))
        {
            if (Application.isPlaying)
            {
                controller.PlayCurrentTarget();
            }
            else
            {
                Debug.LogWarning("Cannot play dialogue in Edit Mode. Please enter Play Mode.");
            }
        }

        if (GUILayout.Button("Play Section"))
        {
            if (Application.isPlaying)
            {
                controller.PlaySection();
            }
        }

        if (GUILayout.Button("Refresh ID List"))
        {
            controller.RefreshIDList();
        }

        // Dropdown selection helper
        if (controller.AvailableIDs != null && controller.AvailableIDs.Count > 0)
        {
            int index = controller.AvailableIDs.IndexOf(controller.TargetID);
            if (index == -1) index = 0;

            int newIndex = EditorGUILayout.Popup("Quick Select", index, controller.AvailableIDs.ToArray());
            if (newIndex >= 0 && newIndex < controller.AvailableIDs.Count)
            {
                controller.TargetID = controller.AvailableIDs[newIndex];
                EditorUtility.SetDirty(controller);
            }
        }
    }
}
