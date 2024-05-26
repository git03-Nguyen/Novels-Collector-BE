﻿using NovelsCollector.SDK;

namespace NovelsCollector.Core.Plugins
{
    public interface IPluginManager
    {
        Dictionary<string, IPlugin> Plugins { get; }
        void ReloadPlugins();
    }
}
