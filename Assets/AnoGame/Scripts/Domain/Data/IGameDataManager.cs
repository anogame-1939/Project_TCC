using System;
using System.Threading.Tasks;

namespace AnoGame.Domain.Data.Services
{
    public interface IGameDataRepository
    {
        Task SaveDataAsync<T>(T data);

        Task<T> LoadDataAsync<T>() where T : class;

        
    }
}
