using NovelsCollector.Domain.Resources.Novels;

namespace NovelsCollector.Domain.Resources
{
    public interface INovelsContainer : IResource
    {
        public Novel[]? Novels { get; set; }
    }
}
