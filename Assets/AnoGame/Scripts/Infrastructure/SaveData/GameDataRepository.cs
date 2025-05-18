using System.Threading.Tasks;
using AnoGame.Infrastructure.Persistence;
using AnoGame.Domain.Data.Models;
using AnoGame.Domain.Data.Services;

namespace AnoGame.Infrastructure.SaveData
{
    public class GameDataRepository : IGameDataRepository
    {
        private readonly AsyncJsonDataManager _jsonManager;
        private const string SaveFileName = "savedata.json";

        public GameDataRepository()
        {
            _jsonManager = new AsyncJsonDataManager();
        }

        public async Task SaveDataAsync<GameData>(GameData data)
        {
            await _jsonManager.SaveDataAsync(SaveFileName, data);
        }

        public async Task<GameData> LoadDataAsync()
        {
            return await _jsonManager.LoadDataAsync<GameData>(SaveFileName);
        }

    }
}