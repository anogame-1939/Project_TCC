using UnityEngine;
using Unity.TinyCharacterController.Brain;
using AnoGame.Data;
using AnoGame.Infrastructure;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AnoGame.Application.Story
{
    public class PlayerSpawnManager : SingletonMonoBehaviour<PlayerSpawnManager>
    {   
        private const string TAG_START_POINT = "StartPoint";
        private const string TAG_RETRY_POINT = "RetryPoint";
        private Transform _currentRetryPoint;

        private Transform GetStartPoint()
        {
            var startPoint = GameObject.FindWithTag(TAG_START_POINT);
            if (startPoint == null)
            {
                Debug.LogError("StartPointタグのついたオブジェクトが見つかりません。");
            }
            return startPoint?.transform;
        }

        private Transform GetRetryPoint()
        {
            var retryPoint = GameObject.FindWithTag(TAG_RETRY_POINT);
            return retryPoint?.transform;
        }

        private GameObject GetPlayer()
        {
            return GameObject.FindWithTag(SLFBRules.TAG_PLAYER);
        }

        private void Awake()
        {
            // StoryManager.Instance.ChapterLoaded += OnChapterLoaded;
        }

        private void OnDestroy()
        {
        }

        // ChapterLoadedイベントハンドラを修正
        public void OnChapterLoaded(bool useRetryPoint)
        {
            if (useRetryPoint)
            {
                Debug.Log("リトライポイントからチャプターを開始します。");
                SpawnPlayerAtRetryPoint();
            }
            else
            {
                Debug.Log("スタートポイントからチャプターを開始します。");
                SpawnPlayerAtStart();
            }
        }

        private void OnChapterStartRequested(bool useRetryPoint)
        {
            if (useRetryPoint)
            {
                Debug.Log("リトライポイントからチャプターを開始します。");
                SpawnPlayerAtRetryPoint();
            }
            else
            {
                Debug.Log("スタートポイントからチャプターを開始します。");
                SpawnPlayerAtStart();
            }
        }

        private void OnChapterLoaded()
        {
            Debug.Log("新しいチャプターを開始します。プレイヤーをスタートポイントに配置します。");
            SpawnPlayerAtStart();
        }

        // 基本的なスポーン処理 - チャプター開始時やリスポーン時に使用
        public void SpawnPlayerAtStart()
        {
            var startPoint = GetStartPoint();
            if (startPoint == null) return;

            WarpPlayerTo(startPoint.position, startPoint.rotation);
        }

        // 外部から呼び出し専用のリトライポイントワープ
        public void SpawnPlayerAtRetryPoint()
        {
            // リトライポイント取得
            Transform targetPoint = _currentRetryPoint ?? GetRetryPoint();

            // 前回セーブした位置を保存
            var currentGameData = GameManager.instance.CurrentGameData;

            // リトライポイントが存在しない場合は前回セーブした位置からスポーンする
            if (targetPoint == null)
            {
                var position = currentGameData.playerPosition.position.ToVector3();
                var rotation = currentGameData.playerPosition.rotation.ToQuaternion();
                WarpPlayerTo(position, rotation);
            }
            else
            {
                WarpPlayerTo(targetPoint.position, targetPoint.rotation);
            }
        }
        

        private void SpawnPlayer(GameData gameData)
        {
            // プレイヤーを前回終了位置に配置
            var player = GameObject.FindGameObjectWithTag(SLFBRules.TAG_PLAYER);
            if (player != null)
            {
                var brain = player.GetComponent<CharacterBrain>();
                brain.Warp(gameData.playerPosition.position.ToVector3(), gameData.playerPosition.rotation.ToQuaternion());
            }
        }

        private void WarpPlayerTo(Vector3 position, Quaternion rotation)
        {
            var player = GetPlayer();
            if (player == null)
            {
                Debug.LogError("プレイヤーの取得に失敗しました。");
                return;
            }

            var brain = player.GetComponent<CharacterBrain>();
            if (brain != null)
            {
                brain.Warp(position, rotation);
                Debug.Log($"プレイヤーを ({position}) の位置に移動しました。");
            }
            else
            {
                Debug.LogError("CharacterBrainコンポーネントの取得に失敗しました。");
            }
        }

        // リトライポイントの設定 - 外部から必要に応じて呼び出し
        public void SetRetryPoint(Transform point)
        {
            if (point != null)
            {
                _currentRetryPoint = point;
                Debug.Log($"リトライポイントを {point.name} に設定しました。");
            }
        }

        // 現在位置をリトライポイントとして設定 - 外部から必要に応じて呼び出し
        public void SetCurrentPositionAsRetryPoint()
        {
            var player = GetPlayer();
            if (player == null)
            {
                Debug.LogError("プレイヤーの取得に失敗しました。");
                return;
            }

            GameObject retryPointObj = new GameObject("DynamicRetryPoint");
            retryPointObj.tag = TAG_RETRY_POINT;
            retryPointObj.transform.position = player.transform.position;
            retryPointObj.transform.rotation = player.transform.rotation;

            _currentRetryPoint = retryPointObj.transform;
            Debug.Log("現在位置をリトライポイントとして設定しました。");
        }

#if UNITY_EDITOR
        [ContextMenu("Reset To Start Point")]
        public void ExecuteSpawnAtStart()
        {
            SpawnPlayerAtStart();
        }

        [ContextMenu("Use Retry Point")]
        public void ExecuteSpawnAtRetryPoint()
        {
            SpawnPlayerAtRetryPoint();
        }

        [ContextMenu("Set Current As Retry Point")]
        public void ExecuteSetCurrentAsRetryPoint()
        {
            SetCurrentPositionAsRetryPoint();
        }

        [CustomEditor(typeof(PlayerSpawnManager))]
        public class PlayerSpawnManagerEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();

                PlayerSpawnManager manager = (PlayerSpawnManager)target;

                if (GUILayout.Button("Reset To Start Point"))
                {
                    manager.SpawnPlayerAtStart();
                }

                if (GUILayout.Button("Use Retry Point"))
                {
                    manager.SpawnPlayerAtRetryPoint();
                }

                if (GUILayout.Button("Set Current As Retry Point"))
                {
                    manager.SetCurrentPositionAsRetryPoint();
                }
            }
        }
#endif
    }
}