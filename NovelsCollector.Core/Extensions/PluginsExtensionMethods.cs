using NovelsCollector.Core.Plugins;

namespace NovelsCollector.Core.Extensions
{
    public static class PluginsExtensionMethods
    {
        public static IServiceCollection AddPluginManager(this IServiceCollection services)
        {
            services.AddSingleton<IPluginManager, SourcePluginsManager>();
            return services;
        }

        public static IApplicationBuilder UsePluginManager(this IApplicationBuilder app)
        {
            IPluginManager pluginManager = app.ApplicationServices.GetService<IPluginManager>();
            pluginManager.LoadPlugins();
            return app;
        }

    }
}
