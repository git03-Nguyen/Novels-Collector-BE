namespace NovelsCollector.Domain.Entities.Plugins.Exporters
{
    using NovelsCollector.Domain.Resources.Novels;
    using System.IO;
    using System.Threading.Tasks;

    public interface IExporterFeature : IPluginFeature
    {
        // export the novel to the file
        public Task Export(Novel novel, Stream stream);

        // ... More
    }
}
