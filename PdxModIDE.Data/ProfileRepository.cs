using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PdxModIDE.Domain;

namespace PdxModIDE.Data
{
    public class ProfileRepository : IProfileRepository
    {
        public Task<List<Domain.Profile>> GetAllAsync()
        {
            var profiles = DataLoader.LoadProfiles();
            var domainProfiles = profiles.Select(p => new Domain.Profile
            {
                Id = Guid.NewGuid().ToString(),
                Name = p.Name,
                Game = p.Game,
                GameRoot = p.GameRoot,
                ModRoot = p.ModRoot,
                BackupRoot = p.BackupRoot,
                YearOffset = p.YearOffset,
                ModuleIds = p.Modules ?? new List<string>(),
                FileIds = p.Files ?? new List<string>()
            }).ToList();

            return Task.FromResult(domainProfiles);
        }

        public Task<Domain.Profile?> GetByIdAsync(string id)
        {
            var profiles = DataLoader.LoadProfiles();
            foreach (var profile in profiles)
            {
                if (profile.Name == id)
                {
                    return Task.FromResult<Domain.Profile?>(new Domain.Profile
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = profile.Name,
                        Game = profile.Game,
                        GameRoot = profile.GameRoot,
                        ModRoot = profile.ModRoot,
                        BackupRoot = profile.BackupRoot,
                        YearOffset = profile.YearOffset,
                        ModuleIds = profile.Modules ?? new List<string>(),
                        FileIds = profile.Files ?? new List<string>()
                    });
                }
            }
            return Task.FromResult<Domain.Profile?>(null);
        }

        public Task<Domain.Profile?> GetByNameAsync(string name)
        {
            var profiles = DataLoader.LoadProfiles();
            var profile = profiles.FirstOrDefault(p => p.Name == name);
            if (profile == null)
                return Task.FromResult<Domain.Profile?>(null);

            return Task.FromResult<Domain.Profile?>(new Domain.Profile
            {
                Id = Guid.NewGuid().ToString(),
                Name = profile.Name,
                Game = profile.Game,
                GameRoot = profile.GameRoot,
                ModRoot = profile.ModRoot,
                BackupRoot = profile.BackupRoot,
                YearOffset = profile.YearOffset,
                ModuleIds = profile.Modules ?? new List<string>(),
                FileIds = profile.Files ?? new List<string>()
            });
        }

        public Task<Domain.Profile> CreateAsync(string id, string name, string game, string gameRoot,
                                                string modRoot, string backupRoot, int yearOffset,
                                                List<string> moduleIds, List<string> fileIds)
        {
            var profile = new Domain.Profile
            {
                Id = id,
                Name = name,
                Game = game,
                GameRoot = gameRoot,
                ModRoot = modRoot,
                BackupRoot = backupRoot,
                YearOffset = yearOffset,
                ModuleIds = moduleIds,
                FileIds = fileIds
            };

            var profiles = DataLoader.LoadProfiles();
            profiles.Add(new Profile
            {
                Name = profile.Name,
                Game = profile.Game,
                GameRoot = profile.GameRoot,
                ModRoot = profile.ModRoot,
                BackupRoot = profile.BackupRoot,
                YearOffset = profile.YearOffset,
                Modules = profile.ModuleIds,
                Files = profile.FileIds
            });

            DataLoader.SaveProfiles(profiles);
            return Task.FromResult(profile);
        }

        public Task<bool> UpdateAsync(Domain.Profile profile)
        {
            var profiles = DataLoader.LoadProfiles();
            var existingProfile = profiles.FirstOrDefault(p => p.Name == profile.Name);
            if (existingProfile == null)
                return Task.FromResult(false);

            existingProfile.Name = profile.Name;
            existingProfile.Game = profile.Game;
            existingProfile.GameRoot = profile.GameRoot;
            existingProfile.ModRoot = profile.ModRoot;
            existingProfile.BackupRoot = profile.BackupRoot;
            existingProfile.YearOffset = profile.YearOffset;
            existingProfile.Modules = profile.ModuleIds;
            existingProfile.Files = profile.FileIds;

            DataLoader.SaveProfiles(profiles);
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(string id)
        {
            var profiles = DataLoader.LoadProfiles();
            var profileToRemove = profiles.FirstOrDefault(p => p.Name == id);
            if (profileToRemove == null)
                return Task.FromResult(false);

            profiles.Remove(profileToRemove);
            DataLoader.SaveProfiles(profiles);
            return Task.FromResult(true);
        }

        public Task SaveAllAsync()
        {
            return Task.CompletedTask;
        }
    }
}
