using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovelsCollector.Domain.Entities.Plugins.Exporters
{
    public interface IExporterPlugin
    {
        public string? Extension { get; set; }
    }
}
