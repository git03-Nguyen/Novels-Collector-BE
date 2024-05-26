using NovelsCollector.Core.Models.Novels;

namespace NovelsCollector.Core.Plugins
{
    public interface ISourcePluginManager : IPluginManager
    {
        //Task<List<Novel>> ExecuteSearch(string? keyword, string? author, string? year);
        Task<string> ExecuteSearch(string? keyword, string? author, string? year);
    }
}
