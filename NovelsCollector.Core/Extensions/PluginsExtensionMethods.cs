using NovelsCollector.Core.Plugins;

namespace NovelsCollector.Core.Extensions
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
