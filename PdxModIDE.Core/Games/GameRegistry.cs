using PdxModIDE.Domain.Interfaces;

namespace PdxModIDE.Core.Games
{
    public static class GameRegistry
    {
        private static readonly Dictionary<string, IGamePlugin> _plugins = new();

        public static void Register(IGamePlugin plugin)
        {
            _plugins[plugin.GameKey] = plugin;
        }

        public static IGamePlugin? GetPlugin(string gameKey)
        {
            return _plugins.TryGetValue(gameKey, out var plugin) ? plugin : null;
        }

        public static IReadOnlyDictionary<string, IGamePlugin> AllPlugins =>
            _plugins.AsReadOnly();

        public static IGamePlugin? DetectGame(string gameRoot)
        {
            foreach (var plugin in _plugins.Values.OrderByDescending(p => p.GameKey.Length))
            {
                if (plugin.CanHandleGame(gameRoot))
                    return plugin;
            }
            return null;
        }
    }
}
