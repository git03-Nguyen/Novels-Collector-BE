using NovelsCollector.Core.Services.Plugins.Sources;

namespace NovelsCollector.Core.Utils
{
    public static class PluginsExtensionMethods
    {
        public static IServiceCollection AddPlugins(this IServiceCollection services)
        {
            services.AddSingleton<ISourcePluginManager, SourcePluginsManager>();
            return services;
        }

        public static IApplicationBuilder UsePlugins(this IApplicationBuilder app)
        {
            ISourcePluginManager pluginManager = app.ApplicationServices.GetService<ISourcePluginManager>();
            return app;
        }

    }
}
