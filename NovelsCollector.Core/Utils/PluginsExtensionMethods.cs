using NovelsCollector.Core.Services.Plugins;

namespace NovelsCollector.Core.Utils
{
    public static class PluginsExtensionMethods
    {
        public static IServiceCollection AddPlugins(this IServiceCollection services)
        {
            services.AddSingleton<MongoDbContext>();
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
