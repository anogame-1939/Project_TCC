using UnityEngine;
using DG.Tweening;

namespace AnoGame.Common.Movement
{
    public class ObjectMover : MonoBehaviour
    {
        [SerializeField] private float duration = 1f;
        [SerializeField] private Transform targetTransform; // 移動先のTransform
        [SerializeField] private bool useCustomPosition = false; // カスタム位置を使用するかのフラグ
        [SerializeField] private Vector3 customTargetPosition; // カスタムの移動先位置
        [SerializeField] private Ease easeType = Ease.InOutQuad;
        [SerializeField] private float delayBetweenMoves = 0.5f;

        private Vector3 initialPosition;

        void Awake()
        {
            initialPosition = transform.position;
        }

        void Start()
        {
            MoveToTarget();
        }

        // 現在の目標位置を取得
        private Vector3 GetTargetPosition()
        {
            if (useCustomPosition)
                return customTargetPosition;
            
            if (targetTransform != null)
                return targetTransform.position;
            
            Debug.LogWarning("Target Transform is not set and custom position is not enabled.");
            return transform.position;
        }

        [ContextMenu("Move To Target")]
        public void MoveToTarget()
        {
            transform.DOKill();
            transform.DOMove(GetTargetPosition(), duration)
                .SetEase(easeType)
                .OnComplete(() => Debug.Log("移動完了!"))
                .OnStart(() => Debug.Log("移動開始!"));
        }

        [ContextMenu("Reset Position")]
        public void ResetPosition()
        {
            transform.DOKill();
            transform.DOMove(initialPosition, duration)
                .SetEase(easeType)
                .OnComplete(() => Debug.Log("初期位置に戻りました"))
                .OnStart(() => Debug.Log("リセット開始"));
        }

        [ContextMenu("Execute Full Sequence")]
        public void ExecuteFullSequence()
        {
            transform.DOKill();
            
            Sequence sequence = DOTween.Sequence();
            
            sequence.AppendCallback(() => Debug.Log("シーケンス開始"))
                .Append(transform.DOMove(GetTargetPosition(), duration).SetEase(easeType))
                .AppendCallback(() => Debug.Log("目標位置への移動完了"))
                .AppendInterval(delayBetweenMoves)
                .Append(transform.DOMove(initialPosition, duration).SetEase(easeType))
                .AppendCallback(() => Debug.Log("シーケンス完了"));
        }

        public void StartNewMovement(Vector3 newTarget, float newDuration, Ease newEaseType)
        {
            transform.DOKill();
            transform.DOMove(newTarget, newDuration)
                .SetEase(newEaseType);
        }

        // 外部からターゲットを設定するためのメソッド
        public void SetTargetTransform(Transform newTarget)
        {
            targetTransform = newTarget;
        }

        // 外部からカスタム位置を設定するためのメソッド
        public void SetCustomTargetPosition(Vector3 newPosition)
        {
            customTargetPosition = newPosition;
            useCustomPosition = true;
        }

        // カスタム位置の使用を切り替えるメソッド
        public void ToggleUseCustomPosition(bool useCustom)
        {
            useCustomPosition = useCustom;
        }

#if UNITY_EDITOR
        [UnityEditor.CustomEditor(typeof(ObjectMover))]
        public class ObjectMoverEditor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                ObjectMover mover = (ObjectMover)target;

                // Target Transform と UseCustomPosition の設定を先に表示
                UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("targetTransform"));
                UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("useCustomPosition"));

                // useCustomPosition が true の時のみ customTargetPosition を表示
                if (mover.useCustomPosition)
                {
                    UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("customTargetPosition"));
                }

                // その他のプロパティを表示
                UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("duration"));
                UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("easeType"));
                UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("delayBetweenMoves"));

                serializedObject.ApplyModifiedProperties();

                UnityEditor.EditorGUILayout.Space();
                UnityEditor.EditorGUILayout.LabelField("デバッグ操作", UnityEditor.EditorStyles.boldLabel);

                // 現在の目標位置を表示
                Vector3 currentTarget = mover.GetTargetPosition();
                UnityEditor.EditorGUILayout.Vector3Field("現在の目標位置", currentTarget);

                // 移動時間の情報を表示
                float totalTime = mover.duration * 2 + mover.delayBetweenMoves;
                UnityEditor.EditorGUILayout.HelpBox(
                    $"全シーケンスの実行時間: {totalTime}秒\n" +
                    $"- 移動時間: {mover.duration}秒 x 2\n" +
                    $"- 待機時間: {mover.delayBetweenMoves}秒",
                    UnityEditor.MessageType.Info
                );

                if (GUILayout.Button("移動実行"))
                {
                    mover.MoveToTarget();
                }

                if (GUILayout.Button("位置をリセット"))
                {
                    mover.ResetPosition();
                }

                if (GUILayout.Button("移動→リセットを実行"))
                {
                    mover.ExecuteFullSequence();
                }
            }
        }
#endif
    }
}