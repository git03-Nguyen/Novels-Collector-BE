﻿using NovelsCollector.SDK.Models;

namespace NovelsCollector.SDK.Plugins.SourcePlugins
{
    public interface ISourcePlugin : IPlugin
    {
        public string Url { get; }
        public Task<Novel[]> CrawlSearch(string? keyword);
        public Task<Novel> CrawlDetail(string novelSlug);
        public Task<string> CrawChapter(Novel novel, Chapter chapter);

        // ... More
    }
}
