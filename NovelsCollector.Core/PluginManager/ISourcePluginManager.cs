using NovelsCollector.SDK.Models;

namespace NovelsCollector.Core.PluginManager
{
    public interface ISourcePluginManager : IPluginManager
    {
        public Task<List<Novel>> ExecuteSearch(string? keyword, string? author, string? year);

        public Task<Novel> GetNovel(string url);

        //Task<string> ExecuteSearch(string? keyword, string? author, string? year);
        //Task<string> GetNovel(string url);
    }
}
