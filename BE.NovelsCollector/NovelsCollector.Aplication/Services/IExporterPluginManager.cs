using NovelsCollector.Domain.Resources.Novels;

namespace NovelsCollector.Aplication.Services
{
    public interface IExporterPluginManager
    {
        public Task<string?> Export(string pluginName, Novel novel, Stream outputStream);
    }
}
