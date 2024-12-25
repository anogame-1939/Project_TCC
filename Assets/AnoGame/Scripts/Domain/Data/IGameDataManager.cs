using System;
using System.Threading.Tasks;

namespace AnoGame.Domain.Data.Services
{
    public interface IGameDataManager
    {
        Task SaveDataAsync<T>(T data);
        Task SaveDataAsync<T>(string fileName, T data);

        Task<T> LoadDataAsync<T>() where T : class;

        Task<T> LoadDataAsync<T>(string fileName) where T : class;
        
    }
}
