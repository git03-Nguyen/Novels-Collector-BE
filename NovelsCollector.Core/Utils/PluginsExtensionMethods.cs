using NovelsCollector.Core.Services.Plugins;

namespace NovelsCollector.Core.Utils
{
    public static class PluginsExtensionMethods
    {
        public static IServiceCollection AddPlugins(this IServiceCollection services)
        {
            services.AddSingleton<SourcePluginsManager>();
            return services;
        }

        public static IApplicationBuilder UsePlugins(this IApplicationBuilder app)
        {
            var pluginManager = app.ApplicationServices.GetService<SourcePluginsManager>();
            return app;
        }

    }
}
