using NovelsCollector.Domain.Entities.Plugins.Exporters;
using NovelsCollector.Domain.Resources.Novels;

namespace Exporter.TemplatePlugin
{
    public class TemplatePlugin : IExporterFeature
    {
        public Task Export(Novel novel, Stream stream)
        {
            throw new NotImplementedException();
        }
    }
}
