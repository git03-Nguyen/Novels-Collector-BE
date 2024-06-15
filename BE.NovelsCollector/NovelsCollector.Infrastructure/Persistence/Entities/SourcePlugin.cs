using NovelsCollector.Domain.Entities.Plugins;
using NovelsCollector.Domain.Entities.Plugins.Sources;
using NovelsCollector.Domain.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovelsCollector.Infrastructure.Persistence.Entities
{
    public class SourcePlugin : ISourcePlugin
    {
        public string? Url { get; set; }
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Version { get; set; }
        public string? Author { get; set; }
        public string? Assembly { get; set; }
        public string? Icon { get; set; }
        public bool? IsLoaded { get; set; }
        public IPluginFeature? PluginInstance { get; set; }
        public PluginLoadContext? LoadContext { get; set; }
        // Other specific properties for source plugins
    }
}
