using NovelsCollector.SDK.Models;

namespace NovelsCollector.SDK.Plugins.ExporterPlugins
{
    public interface IExporterPlugin : IPlugin
    {
        // export the novel to the file
        public Task<Stream> Export(Novel novel);

        // ... More
    }
}
