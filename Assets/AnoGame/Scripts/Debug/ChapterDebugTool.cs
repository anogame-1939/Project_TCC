using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System;
using System.IO;
using AnoGame.Domain.Data.Models;
using AnoGame.Infrastructure.Persistence; // AsyncJsonDataManager を利用
using AnoGame.Application;
using TMPro;
using AnoGame.Application.Story.Manager;

namespace AnoGame.SLFBDebug
{
    public class ChapterDebugTool : MonoBehaviour
    {
        [SerializeField]
        private string folderPath = "";    // JSONファイルを格納しているフォルダへのパス
        [SerializeField]
        private string filePattern = "savedata_*.json"; // 例：savedata_1-5.jsonなど
        [SerializeField]
        private Button buttonPrefab;       // プレハブ化したButton
        [SerializeField]
        private Transform buttonParent;    // 生成先の親オブジェクト(ScrollViewなど)

        private AsyncJsonDataManager _jsonManager;

        private void Awake()
        {
            // AsyncJsonDataManagerのインスタンスを生成
            _jsonManager = new AsyncJsonDataManager();

            // persistentDataPathをフォルダパスとして利用
            folderPath = UnityEngine.Application.persistentDataPath;
            Debug.Log($"Application.persistentDataPath: {folderPath}");

            // 指定フォルダからファイル一覧を取得し、ボタンを動的に生成
            CreateButtonsForJsonFiles();
        }

        /// <summary>
        /// 指定フォルダ内の「savedata_*.json」ファイルに対してボタンを生成する
        /// </summary>
        private void CreateButtonsForJsonFiles()
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                Debug.LogError("フォルダパスが指定されていません。");
                return;
            }

            if (buttonPrefab == null || buttonParent == null)
            {
                Debug.LogError("Buttonプレハブまたはボタンの配置先が設定されていません。");
                return;
            }

            try
            {
                // 指定フォルダから指定パターンのファイル一覧を取得
                string[] files = Directory.GetFiles(folderPath, filePattern);

                foreach (var file in files)
                {
                    // ファイル名のみを取得
                    string fileName = Path.GetFileName(file); // 例："savedata_1-5.json"
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName); // 例："savedata_1-5"

                    // "savedata_1-5" の後ろの部分を取り出す
                    string[] splitName = fileNameWithoutExt.Split('_');
                    string displayName = (splitName.Length >= 2) ? splitName[1] : fileNameWithoutExt;

                    // ボタンを生成
                    Button newButton = Instantiate(buttonPrefab, buttonParent);
                    // TextMeshProのテキストコンポーネントを取得して設定
                    TMP_Text buttonText = newButton.GetComponentInChildren<TMP_Text>();
                    if (buttonText != null)
                    {
                        buttonText.text = displayName;
                    }

                    // クリック時の処理を設定
                    newButton.onClick.AddListener(() =>
                    {
                        // クリックされたときにJSONを読み込み、チャプターを更新
                        LoadChapterDataAsync(file).Forget();
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"ファイル探索中にエラーが発生しました: {ex.Message}");
            }
        }

        /// <summary>
        /// 指定のJSONファイルからGameDataを読み込み、現在のゲームデータを更新
        /// </summary>
        private async UniTask LoadChapterDataAsync(string filePath)
        {
            try
            {
                // JSONファイルからGameDataを非同期で読み込む
                GameData loadedData = await _jsonManager.LoadDataAsync<GameData>(filePath);

                if (loadedData == null)
                {
                    Debug.LogError($"JSONファイルの読み込みに失敗しました。ファイル名: {filePath}");
                    return;
                }

                // GameManager2のUpdateGameStateメソッドを呼び出して、現在のゲームデータを更新
                GameManager2.Instance.UpdateGameState(loadedData);

                Debug.Log($"JSONファイル({filePath})からデータを読み込みました。");
            }
            catch (Exception ex)
            {
                Debug.LogError($"チャプター読み込み中にエラーが発生しました: {ex.Message}");
            }
        }

        public void Save()
        {
            // StoryStateManager.Instance.UpdatePlayerPosition();
            GameManager2.Instance.SaveData();

            Debug.Log("保存");

        }
    }
}
