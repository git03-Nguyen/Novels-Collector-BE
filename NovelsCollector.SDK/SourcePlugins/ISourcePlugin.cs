using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovelsCollector.SDK.SourcePlugins
{
    public interface ISourcePlugin : IPlugin
    {
        string Url { get; }
        Task<string> Search(string? keyword, string? author, string? year);
        Task<string> GetNovel(string url);
    }
}
