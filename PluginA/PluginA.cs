using NovelsCollector.SDK.SourcePlugins;

namespace PluginA
{
    public class PluginA : ISourcePlugin
    {
        public string Name => "PluginA";
        public string Url => "https://truyenfull.vn/";

        public async Task<string> GetNovel(string url)
        {
            // fetch url by HttpClient
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            return content;
        }

        public async Task<string> Search(string? keyword, string? author, string? year)
        {
            // fetch https://truyenfull.vn/tim-kiem/?tukhoa=keyword&tacgia=author&nam=year by HttpClient
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"{Url}tim-kiem/?tukhoa={keyword}");
            var content = await response.Content.ReadAsStringAsync();
            return content;
        }
    }
}
