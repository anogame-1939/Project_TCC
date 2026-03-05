using AnoGame.Domain.Data.Models;
using System.Threading.Tasks;

namespace AnoGame.Domain.Data.Services
{
    public interface IGameDataRepository
    {
        Task SaveDataAsync<GameData>(GameData data);

        Task<GameData> LoadDataAsync();
    }
}
