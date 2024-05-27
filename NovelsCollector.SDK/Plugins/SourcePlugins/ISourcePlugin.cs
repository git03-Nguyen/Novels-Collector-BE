using NovelsCollector.SDK.Models;

namespace NovelsCollector.SDK.Plugins.SourcePlugins
{
    public interface ISourcePlugin : IPlugin
    {
        public string Url { get; }
        public Task<Novel[]> Search(string? keyword, string? author, string? year);
        public Task<Novel> GetNovel(string url);

        // ... More
    }
}
