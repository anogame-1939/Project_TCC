using System.Threading.Tasks;
using AnoGame.Infrastructure.Persistence;
using AnoGame.Domain.Data.Models;
using AnoGame.Domain.Data.Services;

namespace AnoGame.Infrastructure.SaveData
{
    public class SettingsDataRepository : ISettingsDataRepository
    {
        private readonly AsyncJsonDataManager _jsonManager;
        private const string SaveFileName = "settings.json";

        public SettingsDataRepository()
        {
            _jsonManager = new AsyncJsonDataManager();
        }

        public async Task SaveDataAsync<SettingsData>(SettingsData data)
        {
            await _jsonManager.SaveDataAsync(SaveFileName, data);
        }

        public async Task<SettingsData> LoadDataAsync()
        {
            return await _jsonManager.LoadDataAsync<SettingsData>(SaveFileName);
        }

    }
}