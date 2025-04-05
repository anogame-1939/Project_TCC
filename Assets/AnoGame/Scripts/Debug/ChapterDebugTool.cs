using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using AnoGame.Domain.Data.Models;
using AnoGame.Infrastructure.Persistence; // AsyncJsonDataManager を利用
using AnoGame.Application; // GameManager2 を利用

namespace AnoGame.Application.SLFBDebug
{
    public class ChapterDebugTool : MonoBehaviour
    {
        // 読み込むJSONファイル名またはパス（例："chapterData.json"）
        [SerializeField]
        private string jsonFilePath = "chapterData.json";

        // 開始したいチャプター番号
        [SerializeField]
        private int startChapter = 1;

        private AsyncJsonDataManager _jsonManager;

        private void Awake()
        {
            // AsyncJsonDataManagerのインスタンスを生成
            _jsonManager = new AsyncJsonDataManager();
        }

        private void Update()
        {
            // デバッグ用：Cキーが押されたらJSONファイルを読み込み、指定チャプターでスタート
            if (Input.GetKeyDown(KeyCode.C))
            {
                LoadChapterDataAsync().Forget();
            }
        }

        /// <summary>
        /// 指定のJSONファイルからGameDataを読み込み、開始チャプターを更新してゲームデータに反映する
        /// </summary>
        private async UniTask LoadChapterDataAsync()
        {
            try
            {
                // JSONファイルからGameDataを非同期で読み込む
                GameData loadedData = await _jsonManager.LoadDataAsync<GameData>(jsonFilePath);

                if (loadedData == null)
                {
                    Debug.LogError($"JSONファイルの読み込みに失敗しました。ファイル名: {jsonFilePath}");
                    return;
                }

                // StoryProgressは直接代入できないため、UpdateStoryProgressメソッドを利用して更新
                loadedData.UpdateStoryProgress(new StoryProgress(startChapter, 0));

                // GameManager2のUpdateGameStateメソッドを呼び出して、現在のゲームデータを更新
                GameManager2.Instance.UpdateGameState(loadedData);

                Debug.Log($"JSONファイル({jsonFilePath})からデータを読み込み、チャプター {startChapter} でスタートしました。");
            }
            catch (Exception ex)
            {
                Debug.LogError($"チャプター読み込み中にエラーが発生しました: {ex.Message}");
            }
        }
    }
}
