

using HtmlAgilityPack;
using NovelsCollector.SDK.Models;
using NovelsCollector.SDK.SourcePlugins;
using System.Text.RegularExpressions;

namespace SourcePlugin.TruyenFullVn
{
    public class TruyenFullVn : ISourcePlugin
    {
        public string Name => "PluginCrawlTruyenFull";
        public string Url => "https://truyenfull.vn/";

        // ----------------------------------------------------------------
        public async Task<Tuple<Novel[], int>> CrawlSearch(string? keyword, int page = 1)
        {
            // fetch https://truyenfull.vn/tim-kiem/?tukhoa=keyword by HttpClient
            var web = new HtmlWeb();
            var document = await web.LoadFromWebAsync($"{Url}tim-kiem/?tukhoa={keyword}&page={page}");

            // Get Pagination
            int totalPage = 1;
            var paginationElement = document.DocumentNode.QuerySelector("ul.pagination");
            if (paginationElement != null)
            {
                paginationElement.RemoveChild(paginationElement.LastChild);
                var lastChild = paginationElement.LastChild.QuerySelector("a");

                Regex regex = new Regex(@"\d+");

                if (lastChild == null)
                {
                    MatchCollection matches = regex.Matches(paginationElement.LastChild.InnerText);
                    totalPage = int.Parse(matches[0].Value);
                }
                else totalPage = int.Parse(lastChild.Attributes["href"].Value.Replace($"{Url}tim-kiem/?tukhoa={keyword}&page=", ""));
            }

            // Get novels
            var novelElements = document.DocumentNode.QuerySelectorAll(" div.col-truyen-main div.list-truyen div.row");
            var listNovel = new List<Novel>();

            foreach (var novelElement in novelElements)
            {
                Novel novel = new Novel();
                novel.Title = novelElement.QuerySelector("h3.truyen-title").InnerText;
                novel.Slug = novelElement.QuerySelector("h3.truyen-title a").Attributes["href"].Value.Replace(Url, "").Replace("/", "");

                var strAuthor = novelElement.QuerySelector("span.author").InnerText;
                var authorNames = strAuthor?.Split(',').Select(author => author.Trim()).ToArray();
                List<Author> listAuthor = new List<Author>();
                foreach (var name in authorNames)
                {
                    var author = new Author();
                    author.Name = name;
                    listAuthor.Add(author);
                }
                novel.Authors = listAuthor.ToArray();
                novel.Cover = novelElement.QuerySelector("div[data-image]").Attributes["data-image"].Value;

                listNovel.Add(novel);
            }

            return new Tuple<Novel[], int>(listNovel.ToArray(), totalPage);
        }

        public async Task<Novel> CrawlDetail(string novelSlug)
        {
            var web = new HtmlWeb();
            var document = await web.LoadFromWebAsync($"{Url}{novelSlug}/");

            var novel = new Novel();
            novel.Title = document.DocumentNode.QuerySelector("h3.title").InnerText;
            novel.Rating = float.Parse(document.DocumentNode.QuerySelector("span[itemprop='ratingValue']").InnerText);
            novel.Description = document.DocumentNode.QuerySelector("div[itemprop='description']").InnerText;

            // get authors
            var authorElements = document.DocumentNode.QuerySelectorAll("a[itemprop='author']");
            List<Author> listAuthor = new List<Author>();
            foreach (var element in authorElements)
            {
                var author = new Author();
                author.Name = element.Attributes["title"].Value;
                author.Slug = element.Attributes["href"].Value.Replace($"{Url}tac-gia", "").Replace("/", "");
                listAuthor.Add(author);
            }
            novel.Authors = listAuthor.ToArray();

            // get categories
            var genreElements = document.DocumentNode.QuerySelectorAll("div.info a[itemprop='genre']");
            List<Category> listCategory = new List<Category>();
            foreach (var element in genreElements)
            {
                var category = new Category();
                category.Name = element.Attributes["title"].Value;
                category.Slug = element.Attributes["href"].Value.Replace($"{Url}the-loai", "").Replace("/", "");
                listCategory.Add(category);
            }
            novel.Categories = listCategory.ToArray();

            novel.Status = document.DocumentNode.QuerySelector("span.text-success").InnerText.Trim() == "Full"; // check is completed
            novel.Cover = document.DocumentNode.QuerySelector("div.books img").Attributes["src"].Value;

            // get totalPage
            int totalPage = 1;
            var paginationElement = document.DocumentNode.QuerySelector("ul.pagination");
            if (paginationElement != null)
            {
                paginationElement.RemoveChild(paginationElement.LastChild);
                var lastChild = paginationElement.LastChild.QuerySelector("a");
                Regex regex = new Regex(@"\d+");
                if (lastChild == null)
                {
                    MatchCollection matches = regex.Matches(paginationElement.LastChild.InnerText);
                    totalPage = int.Parse(matches[0].Value);
                }
                else
                {
                    MatchCollection matches = regex.Matches(lastChild.Attributes["href"].Value);
                    totalPage = int.Parse(matches[0].Value);
                }
            }

            // list chapter
            List<Chapter> listChapter = new List<Chapter>();
            for (int i = 1; i <= totalPage; i++)
            {
                document = web.Load($"{Url}{novelSlug}/trang-{i}/#list-chapter");

                var chapterElements = document.DocumentNode.QuerySelectorAll("ul.list-chapter li");
                foreach (var element in chapterElements)
                {
                    var chapter = new Chapter();
                    chapter.Title = element.QuerySelector("a").Attributes["title"].Value;
                    chapter.Slug = element.QuerySelector("a").Attributes["href"].Value.Replace($"{Url}{novelSlug}/", "").Replace("/", "");
                    listChapter.Add(chapter);
                }
            }
            novel.Chapters = listChapter.ToArray();

            return novel;
        }

        public async Task<string> CrawChapter(Novel novel, Chapter chapter)
        {
            var web = new HtmlWeb();
            var document = await web.LoadFromWebAsync($"{Url}{novel.Slug}/{chapter.Slug}/");
            var contentElement = document.DocumentNode.QuerySelector("#chapter-c");

            // Remove all ads
            var adsElements = contentElement.QuerySelectorAll("div[class*='ads']");
            foreach (var element in adsElements)
            {
                element.Remove();
            }

            // Get content of chapter in html format
            string content = contentElement.InnerHtml;

            return content;
        }

        public Task<Chapter> CrawlChapter(string novelSlug, string chapterSlug)
        {
            throw new NotImplementedException();
        }
    }
}