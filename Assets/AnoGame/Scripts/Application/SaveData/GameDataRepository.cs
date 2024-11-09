using System.Threading.Tasks;
using AnoGame.Data;
using AnoGame.Infrastructure.Persistence;

namespace AnoGame.Application.SaveData
{
    public class GameDataRepository
    {
        private readonly AsyncJsonDataManager _jsonManager;
        private const string SaveFileName = "savedata.json";

        public GameDataRepository()
        {
            _jsonManager = new AsyncJsonDataManager();
        }

        public void Initialize()
        {
            // 必要に応じてカスタムパスを設定
            // _jsonManager.SetCustomPath(customPath);
        }

        public async Task SaveDataAsync(GameData data)
        {
            await _jsonManager.SaveDataAsync(SaveFileName, data);
        }

        public async Task<GameData> LoadDataAsync()
        {
            return await _jsonManager.LoadDataAsync<GameData>(SaveFileName);
        }
    }
}