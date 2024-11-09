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