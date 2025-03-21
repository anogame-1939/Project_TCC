using AnoGame.Domain.Data.Models;
using System.Threading.Tasks;

namespace AnoGame.Domain.Data.Services
{
    public interface ISettingsDataRepository
    {
        Task SaveDataAsync<SettingsData>(SettingsData data);

        Task<SettingsData> LoadDataAsync();
    }
}
