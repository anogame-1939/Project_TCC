#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class AlignSceneViewToMainCamera
{
    [MenuItem("Tools/Camera/Align SceneView To Main Camera %#k")]
    private static void Align()
    {
        var sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null)
        {
            Debug.LogWarning("SceneView がありません。");
            return;
        }

        var cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("MainCamera がシーンに見つかりません。");
            return;
        }

        // SceneViewの位置・回転を MainCamera に合わせる
        sceneView.pivot = cam.transform.position + cam.transform.forward * 5.0f; 
        sceneView.rotation = cam.transform.rotation;

        // ズーム量（distance）は適宜調整
        sceneView.size = 5f;

        // 変更を即反映
        sceneView.Repaint();
        
        Debug.Log("SceneView を MainCamera の構図に合わせました。");
    }
}
#endif
