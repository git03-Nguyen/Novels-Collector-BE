using NovelsCollector.SDK.Models;

namespace NovelsCollector.SDK.Plugins.ExporterPlugins
{
    public interface IExporterPlugin : IPlugin
    {
        // return the file extension of the exported file
        string FileExtension { get; }

        // export the novels to the file
        Task Export(Novel[] novels, string path);

        // export the chapters to the file
        Task Export(Chapter[] chapters, string path);

        // ... More
    }
}
