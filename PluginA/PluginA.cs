using NovelsCollector.SDK.Models;
using NovelsCollector.SDK.Plugins.SourcePlugins;

namespace PluginA
{
    public class PluginA : ISourcePlugin
    {
        public string Name => "PluginA";
        public string Url => "https://truyenfull.vn/";

        public async Task<Novel> GetNovel(string url)
        {
            return new Novel
            {
                Title = "Title",
                Description = "Description",
                Year = 2021,
                Status = true,
                Rating = 4.5f,
                Authors = new Author[]
                {
                    new Author
                    {
                        Name = "Author"
                    }
                },
                Categories = new Category[]
                {
                    new Category
                    {
                        Name = "Category"
                    }
                },
                Plugins = new ISourcePlugin[]
                {
                    this
                }
            };
        }

        public async Task<Novel[]> Search(string? keyword, string? author, string? year)
        {
            return new Novel[]
            {
                new Novel
                {
                    Title = "Title",
                    Description = "Description",
                    Year = 2021,
                    Status = true,
                    Rating = 4.5f,
                    Authors = new Author[]
                    {
                        new Author
                        {
                            Name = "Author"
                        }
                    },
                    Categories = new Category[]
                    {
                        new Category
                        {
                            Name = "Category"
                        }
                    },
                    Plugins = new ISourcePlugin[]
                    {
                        this
                    }
                }
            };
        }
    }
}
