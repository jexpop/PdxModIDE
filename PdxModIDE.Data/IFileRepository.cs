using System.Collections.Generic;
using System.Threading.Tasks;
using PdxModIDE.Domain;

namespace PdxModIDE.Data
{
    public interface IFileRepository
    {
        Task<Dictionary<string, Dictionary<string, GameFile>>> GetAllAsync();
        Task<Dictionary<string, GameFile>?> GetByGameAsync(string gameKey);
        Task<GameFile?> GetAsync(string gameKey, string fileName);
        Task SetAsync(string gameKey, string fileName, GameFile file);
        Task RemoveAsync(string gameKey, string fileName);
        Task SaveAllAsync();
    }
}
