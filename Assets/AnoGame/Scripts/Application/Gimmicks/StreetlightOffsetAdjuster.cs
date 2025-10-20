using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AnoGame.Application.Gimmicks
{
    [ExecuteInEditMode]
    public class StreetlightOffsetAdjuster : MonoBehaviour
    {
        [Header("光源オブジェクト（子）")]
        [SerializeField] private Transform lightSource;

        [Header("オフセット設定")]
        public Vector3 offsetForward = new Vector3(0, 2f, 0);
        public Vector3 offsetBackward = new Vector3(0, 2f, -0.5f);
        public Vector3 offsetLeft = new Vector3(-0.5f, 2f, 0);
        public Vector3 offsetRight = new Vector3(0.5f, 2f, 0);

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (lightSource == null) return;

            // 向きを元にオフセットを選択
            Vector3 forward = transform.forward;
            Vector3 chosenOffset;

            // 最も近い方向を判定
            if (Vector3.Dot(forward, Vector3.forward) > 0.7f)
                chosenOffset = offsetForward;
            else if (Vector3.Dot(forward, Vector3.back) > 0.7f)
                chosenOffset = offsetBackward;
            else if (Vector3.Dot(forward, Vector3.left) > 0.7f)
                chosenOffset = offsetLeft;
            else
                chosenOffset = offsetRight;

            Undo.RecordObject(lightSource, "Adjust Light Source Offset");
            lightSource.localPosition = chosenOffset;

            EditorUtility.SetDirty(lightSource);
        }
#endif
    }


}
