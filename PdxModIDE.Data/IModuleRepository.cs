using System.Collections.Generic;
using System.Threading.Tasks;
using PdxModIDE.Domain;

namespace PdxModIDE.Data
{
    public interface IModuleRepository
    {
        Task<Dictionary<string, Dictionary<string, Module>>> GetAllAsync();
        Task<Dictionary<string, Module>?> GetByGameAsync(string gameKey);
        Task<Module?> GetAsync(string gameKey, string moduleName);
        Task SetAsync(string gameKey, string moduleName, Module module);
        Task RemoveAsync(string gameKey, string moduleName);
        Task SaveAllAsync();
    }
}
