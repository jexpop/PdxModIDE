using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PdxModIDE.Domain;

namespace PdxModIDE.Data
{
    public class FileRepository : IFileRepository
    {
        public Task<Dictionary<string, Dictionary<string, GameFile>>> GetAllAsync()
        {
            var files = DataLoader.LoadFiles();
            var domainFiles = files.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToDictionary(
                    f => f.Key,
                    f => new GameFile(f.Key, f.Value.Path, f.Value.MapTo)
                )
            );
            return Task.FromResult(domainFiles);
        }

        public Task<Dictionary<string, GameFile>?> GetByGameAsync(string gameKey)
        {
            var files = DataLoader.LoadFiles();
            if (files.TryGetValue(gameKey, out var gameFiles))
            {
                var domainFiles = gameFiles.ToDictionary(
                    f => f.Key,
                    f => new GameFile(f.Key, f.Value.Path, f.Value.MapTo)
                );
                return Task.FromResult<Dictionary<string, GameFile>?>(domainFiles);
            }
            return Task.FromResult<Dictionary<string, GameFile>?>(null);
        }

        public Task<GameFile?> GetAsync(string gameKey, string fileName)
        {
            var files = DataLoader.LoadFiles();
            if (files.TryGetValue(gameKey, out var gameFiles) &&
                gameFiles.TryGetValue(fileName, out var fileConfig))
            {
                return Task.FromResult<GameFile?>(new GameFile(fileName, fileConfig.Path, fileConfig.MapTo));
            }
            return Task.FromResult<GameFile?>(null);
        }

        public Task SetAsync(string gameKey, string fileName, GameFile file)
        {
            var files = DataLoader.LoadFiles();
            if (!files.ContainsKey(gameKey))
            {
                files[gameKey] = new Dictionary<string, FileConfig>();
            }
            files[gameKey][fileName] = new FileConfig
            {
                Path = file.Path,
                MapTo = file.MapTo
            };
            DataLoader.SaveFiles(files);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string gameKey, string fileName)
        {
            var files = DataLoader.LoadFiles();
            if (files.ContainsKey(gameKey) &&
                files[gameKey].ContainsKey(fileName))
            {
                files[gameKey].Remove(fileName);
                DataLoader.SaveFiles(files);
            }
            return Task.CompletedTask;
        }

        public Task SaveAllAsync()
        {
            return Task.CompletedTask;
        }
    }
}
