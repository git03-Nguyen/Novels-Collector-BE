using NovelsCollector.Core.PluginsManager;

namespace NovelsCollector.Core.Utils
{
    public static class PluginsExtensionMethods
    {
        public static IServiceCollection AddPluginManager(this IServiceCollection services)
        {
            services.AddSingleton<ISourcePluginManager, SourcePluginsManager>();
            return services;
        }

        public static IApplicationBuilder UsePluginManager(this IApplicationBuilder app)
        {
            ISourcePluginManager pluginManager = app.ApplicationServices.GetService<ISourcePluginManager>();
            pluginManager.ReloadPlugins();
            return app;
        }

    }
}
