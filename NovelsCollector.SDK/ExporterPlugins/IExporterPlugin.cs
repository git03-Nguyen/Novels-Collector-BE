namespace NovelsCollector.SDK.ExporterPlugins
{
    public interface IExporterPlugin : IPlugin
    {
        // Supported formats
        public string SupportedFormats { get; }

        // Export data to file
        public Task ExportToFile(string filePath, string data);

    }
}
