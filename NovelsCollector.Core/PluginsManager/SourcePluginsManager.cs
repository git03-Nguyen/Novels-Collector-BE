﻿using NovelsCollector.SDK.Models;
using NovelsCollector.SDK.Plugins.SourcePlugins;
using System.Reflection;

namespace NovelsCollector.Core.PluginsManager
{
    public class SourcePluginsManager : ISourcePluginManager
    {
        private static readonly Dictionary<string, ISourcePlugin> plugins = new Dictionary<string, ISourcePlugin>();

        private static readonly string pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");

        public Dictionary<string, ISourcePlugin> Plugins => plugins;

        public SourcePluginsManager()
        {
            ReloadPlugins();
        }

        public void ReloadPlugins()
        {
            Console.WriteLine("Loading plugins...");

            plugins.Clear();

            Console.WriteLine(pluginsPath);
            if (!Directory.Exists(pluginsPath))
            {
                Directory.CreateDirectory(pluginsPath);
            }
            string[] files = Directory.GetFiles(pluginsPath, "*.dll");

            foreach (string file in files)
            {
                Assembly assembly = Assembly.LoadFile(file);
                Type[] types = assembly.GetTypes();

                foreach (Type type in types)
                {
                    if (type.GetInterface("IPlugin") != null)
                    {
                        ISourcePlugin plugin = (ISourcePlugin)Activator.CreateInstance(type);
                        plugins.Add(plugin.Name, plugin);
                    }
                }
            }
        }

        public async Task AddPluginAsync(IFormFile file)
        {
            if (file == null)
            {
                throw new Exception("No file uploaded");
            }

            string path = Path.Combine(pluginsPath, file.FileName);

            // if exists, throw exception
            if (File.Exists(path))
            {
                throw new Exception("Plugin already exists");
            }

            using (FileStream stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            Assembly assembly = Assembly.LoadFile(path);
            Type[] types = assembly.GetTypes();

            foreach (Type type in types)
            {
                if (type.GetInterface("IPlugin") != null)
                {
                    ISourcePlugin plugin = (ISourcePlugin)Activator.CreateInstance(type);
                    plugins.Add(plugin.Name, plugin);
                }
            }
        }

        public async Task RemovePlugin(string name)
        {
            if (!plugins.ContainsKey(name))
            {
                throw new Exception("Plugin not found");
            }

            plugins.Remove(name);
        }

        public async Task<Novel[]> Search(string? keyword, string? author, string? year)
        {
            if (plugins.Count == 0)
            {
                throw new Exception("No plugins loaded");
            }

            var novels = new List<Novel>();
            //int totalPage = 0;

            foreach (var plugin in plugins.Values)
            {
                var novelsResult = await plugin.CrawlSearch(keyword);
                novels.AddRange(novelsResult);
            }

            return novels.ToArray();
        }

        public async Task<Novel> GetNovelDetail(Novel novel)
        {
            if (plugins.Count == 0)
            {
                throw new Exception("No plugins loaded");
            }

            foreach (var plugin in plugins.Values)
            {
                try
                {
                    return await plugin.CrawlDetail(novel);
                }
                catch (Exception)
                {
                    continue;
                }
            }

            throw new Exception("Novel not found");
        }

        public async Task<string> GetChapter(Novel novel, Chapter chapter)
        {
            //if (plugins.Count == 0)
            //{
            //    throw new Exception("No plugins loaded");
            //}

            //foreach (var plugin in plugins.Values)
            //{
            //    try
            //    {
            //        return await plugin.CrawlChapter(novel.Slug, chapter.Slug);
            //    }
            //    catch (Exception)
            //    {
            //        continue;
            //    }
            //}

            throw new Exception("Chapter not found");
        }

        public Task<string> GetChapter(string novelSlug, string chapterSlug)
        {
            throw new NotImplementedException();
        }
    }
}
