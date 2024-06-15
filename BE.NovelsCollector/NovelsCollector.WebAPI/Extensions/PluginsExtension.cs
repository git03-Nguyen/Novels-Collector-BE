using NovelsCollector.Core.Services;

namespace NovelsCollector.WebAPI.Extensions
{
    public static class PluginsExtension
    {
        public static IServiceCollection AddPlugins(this IServiceCollection services)
        {
            services.AddSingleton<SourcePluginsManager>();
            services.AddSingleton<ExporterPluginsManager>();
            return services;
        }

        public static IApplicationBuilder UsePlugins(this IApplicationBuilder app)
        {
            return app;
        }

    }
}
