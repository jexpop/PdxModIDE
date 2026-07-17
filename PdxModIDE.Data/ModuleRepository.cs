using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PdxModIDE.Domain;

namespace PdxModIDE.Data
{
    public class ModuleRepository : IModuleRepository
    {
        public Task<Dictionary<string, Dictionary<string, Module>>> GetAllAsync()
        {
            var modules = DataLoader.LoadModules();
            var domainModules = new Dictionary<string, Dictionary<string, Module>>();

            foreach (var kvp in modules)
            {
                var gameModules = new Dictionary<string, Module>();
                foreach (var moduleKvp in kvp.Value)
                {
                    gameModules[moduleKvp.Key] = new Module(
                        moduleKvp.Key,
                        moduleKvp.Value.Path,
                        moduleKvp.Value.IgnoreExt ?? new List<string>()
                    );
                }
                domainModules[kvp.Key] = gameModules;
            }

            return Task.FromResult(domainModules);
        }

        public Task<Dictionary<string, Module>?> GetByGameAsync(string gameKey)
        {
            var modules = DataLoader.LoadModules();
            if (modules.TryGetValue(gameKey, out var gameModules))
            {
                var domainModules = new Dictionary<string, Module>();
                foreach (var moduleKvp in gameModules)
                {
                    domainModules[moduleKvp.Key] = new Module(
                        moduleKvp.Key,
                        moduleKvp.Value.Path,
                        moduleKvp.Value.IgnoreExt ?? new List<string>()
                    );
                }
                return Task.FromResult<Dictionary<string, Module>?>(domainModules);
            }
            return Task.FromResult<Dictionary<string, Module>?>(null);
        }

        public Task<Module?> GetAsync(string gameKey, string moduleName)
        {
            var modules = DataLoader.LoadModules();
            if (modules.TryGetValue(gameKey, out var gameModules) &&
                gameModules.TryGetValue(moduleName, out var moduleConfig))
            {
                return Task.FromResult<Module?>(new Module(
                    moduleName,
                    moduleConfig.Path,
                    moduleConfig.IgnoreExt ?? new List<string>()
                ));
            }
            return Task.FromResult<Module?>(null);
        }

        public Task SetAsync(string gameKey, string moduleName, Module module)
        {
            var modules = DataLoader.LoadModules();
            if (!modules.ContainsKey(gameKey))
            {
                modules[gameKey] = new Dictionary<string, ModuleConfig>();
            }
            modules[gameKey][moduleName] = new ModuleConfig
            {
                Path = module.Path,
                IgnoreExt = module.IgnoreExtensions.ToList()
            };
            DataLoader.SaveModules(modules);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string gameKey, string moduleName)
        {
            var modules = DataLoader.LoadModules();
            if (modules.ContainsKey(gameKey) &&
                modules[gameKey].ContainsKey(moduleName))
            {
                modules[gameKey].Remove(moduleName);
                DataLoader.SaveModules(modules);
            }
            return Task.CompletedTask;
        }

        public Task SaveAllAsync()
        {
            return Task.CompletedTask;
        }
    }
}
