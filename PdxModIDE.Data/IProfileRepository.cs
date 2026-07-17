using System.Collections.Generic;
using System.Threading.Tasks;
using PdxModIDE.Domain;

namespace PdxModIDE.Data
{
    public interface IProfileRepository
    {
        Task<List<Domain.Profile>> GetAllAsync();
        Task<Domain.Profile?> GetByIdAsync(string id);
        Task<Domain.Profile?> GetByNameAsync(string name);
        Task<Domain.Profile> CreateAsync(string id, string name, string game, string gameRoot,
                                         string modRoot, string backupRoot, int yearOffset,
                                         List<string> moduleIds, List<string> fileIds);
        Task<bool> UpdateAsync(Domain.Profile profile);
        Task<bool> DeleteAsync(string id);
        Task SaveAllAsync();
    }
}
