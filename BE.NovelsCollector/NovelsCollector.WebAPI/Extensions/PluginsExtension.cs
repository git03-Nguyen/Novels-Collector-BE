using NovelsCollector.Application.UseCases.ManagePlugins;
using NovelsCollector.Domain.Entities.Plugins.Exporters;
using NovelsCollector.Domain.Entities.Plugins.Sources;
using NovelsCollector.Infrastructure.Persistence.Entities;

namespace NovelsCollector.WebAPI.Extensions
{
    public static class PluginsExtension
    {
        public static IServiceCollection AddPlugins(this IServiceCollection services)
        {
            // Add the services for the plugins: BasePluginsManager<SourcePlugin, ISourceFeature> and BasePluginsManager<ExporterPlugin, IExporterFeature> as Singleton
            services.AddSingleton<BasePluginsManager<SourcePlugin, ISourceFeature>>();
            services.AddSingleton<BasePluginsManager<ExporterPlugin, IExporterFeature>>();
            return services;
        }

        public static IApplicationBuilder UsePlugins(this IApplicationBuilder app)
        {
            return app;
        }

    }
}
