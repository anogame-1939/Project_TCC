using System.Threading.Tasks;
using AnoGame.Infrastructure.Persistence;
using AnoGame.Domain.Data.Models;

namespace AnoGame.Infrastructure.SaveData
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