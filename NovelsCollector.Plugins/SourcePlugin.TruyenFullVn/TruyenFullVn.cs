using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using NovelsCollector.SDK.Models;
using NovelsCollector.SDK.Plugins.SourcePlugins;
using System.Text.RegularExpressions;

namespace SourcePlugin.TruyenFullVn
{
    public class TruyenFullVn : ISourcePlugin
    {
        public string Name => "PluginCrawlTruyenFull";
        public string Url => "https://truyenfull.vn/";

        /// <summary>
        /// Crawl Detail All Novels. Note: Takes a lot of time and RAM
        /// </summary>
        /// <returns></returns>
        public async Task<Novel[]> CrawlDetailAllNovels()
        {
            var (novels, totalPage) = await CrawlNovels($"{Url}danh-sach/truyen-moi/trang-<page>/");

            List<Novel> listNovel = new List<Novel>();
            for (int i = 1; i <= totalPage; i++)
            {
                var (tempNovels, tempTotalPage) = await CrawlNovels($"{Url}danh-sach/truyen-moi/trang-<page>/", i);

                foreach (var element in tempNovels)
                {
                    var result = await CrawlDetail(element.Slug);
                    listNovel.Add(result);
                }
            }

            return listNovel.ToArray();
        }

        /// <summary>
        /// Crawl novels by search
        /// </summary>
        /// <param name="keyword"></param>
        /// <param name="page"></param>
        /// <returns>First: Novels, Second: total page</returns>
        public async Task<Tuple<Novel[]?, int>> CrawlSearch(string? keyword, int page = 1)
        {
            Console.WriteLine($"CrawlSearch: {keyword}");
            // fetch https://truyenfull.vn/tim-kiem/?tukhoa=keyword
            var reqStr = $"{Url}tim-kiem/?tukhoa={keyword}";
            if (page > 1)
            {
                reqStr += $"&page={page}";
            }
            var result = await CrawlNovels(reqStr, page);
            return result;
        }

        /// <summary>
        /// Crawl hot novels
        /// </summary>
        /// <param name="page"></param>
        /// <returns>First: Novels, Second: total page</returns>
        public async Task<Tuple<Novel[], int>> CrawlHot(int page = 1)
        {
            // fetch https://truyenfull.vn/danh-sach/truyen-hot/ 
            var result = await CrawlNovels($"{Url}danh-sach/truyen-hot/trang-<page>/", page);
            return result;
        }

        /// <summary>
        /// Crawl latest novles
        /// </summary>
        /// <param name="page"></param>
        /// <returns>First: Novels, Second: total page</returns>
        public async Task<Tuple<Novel[], int>> CrawlLatest(int page = 1)
        {
            // fetch https://truyenfull.vn/danh-sach/truyen-moi/
            var result = await CrawlNovels($"{Url}danh-sach/truyen-moi/trang-<page>/", page);
            return result;
        }

        /// <summary>
        /// Crawl completed novels
        /// </summary>
        /// <param name="page"></param>
        /// <returns>First: Novels, Second: total page</returns>
        public async Task<Tuple<Novel[], int>> CrawlCompleted(int page = 1)
        {
            // fetch https://truyenfull.vn/danh-sach/truyen-full/
            var result = await CrawlNovels($"{Url}danh-sach/truyen-full/trang-<page>/", page);
            return result;
        }

        /// <summary>
        /// Crawl novels which are written by this author
        /// </summary>
        /// <param name="author"></param>
        /// <param name="page"></param>
        /// <returns>First: Novels, Second: total page</returns>
        public async Task<Tuple<Novel[], int>> CrawlByAuthor(Author author, int page = 1)
        {
            // fetch https://truyenfull.vn/tac-gia/
            var result = await CrawlNovels($"{Url}tac-gia/{author.Slug}/trang-<page>/", page);
            return result;
        }

        /// <summary>
        /// Crawl novels which have the same category
        /// </summary>
        /// <param name="category"></param>
        /// <param name="page"></param>
        /// <returns>First: Novels, Second: total page</returns>
        public async Task<Tuple<Novel[], int>> CrawlByCategory(Category category, int page = 1)
        {
            // fetch https://truyenfull.vn/the-loai/
            var result = await CrawlNovels($"{Url}the-loai/{category.Slug}/trang-<page>/", page);
            return result;
        }

        public async Task<Category[]> CrawlCategories()
        {
            List<Category> listCategory = new List<Category>();
            var web = new HtmlWeb();
            var document = await web.LoadFromWebAsync(Url);

            var categoryElements = document.DocumentNode.QuerySelectorAll("ul.navbar-nav a[href*='https://truyenfull.vn/the-loai/']");
            foreach (var categoryElement in categoryElements)
            {
                Category category = new Category();
                category.Name = categoryElement?.InnerText;
                category.Slug = categoryElement?.Attributes["href"].Value.Replace("https://truyenfull.vn/the-loai/'", "").Replace("/", "");

                if (listCategory.Count(x => (x.Slug == category.Slug)) == 0)
                {
                    listCategory.Add(category);
                }
            }

            return listCategory.ToArray();
        }

        public async Task<Novel?> CrawlDetail(string novelSlug)
        {
            var web = new HtmlWeb();
            var document = await web.LoadFromWebAsync($"{Url}{novelSlug}/");

            // TODO: Check if novel is not found

            var novel = new Novel();

            novel.Title = document.DocumentNode.QuerySelector("h3.title")?.InnerText;

            var ratingElement = document.DocumentNode.QuerySelector("span[itemprop='ratingValue']");
            if (ratingElement != null) novel.Rating = float.Parse(ratingElement.InnerText);

            novel.Description = document.DocumentNode.QuerySelector("div[itemprop='description']")?.InnerHtml;

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

            novel.Status = document.DocumentNode.QuerySelector("span.text-success")?.InnerText.Trim() == "Full"; // check is completed
            novel.Cover = document.DocumentNode.QuerySelector("div.books img")?.Attributes["src"].Value;

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
                document = await web.LoadFromWebAsync($"{Url}{novel.Slug}/trang-{i}/#list-chapter");

                var chapterElements = document.DocumentNode.QuerySelectorAll("ul.list-chapter li");
                foreach (var element in chapterElements)
                {
                    var chapter = new Chapter();
                    chapter.Title = element.QuerySelector("a").Attributes["title"].Value;
                    chapter.Slug = element.QuerySelector("a").Attributes["href"].Value.Replace($"{Url}{novel.Slug}/", "").Replace("/", "");
                    listChapter.Add(chapter);
                }
            }
            novel.Chapters = listChapter.ToArray();

            return novel;
        }

        public async Task<Chapter?> CrawlChapter(string novelSlug, string chapterSlug)
        {
            var web = new HtmlWeb();
            var document = await web.LoadFromWebAsync($"{Url}{novelSlug}/{chapterSlug}/");

            // TODO: Check if chapter is not found

            var contentElement = document.DocumentNode.QuerySelector("#chapter-c");

            // Remove all ads
            var adsElements = contentElement.QuerySelectorAll("div[class*='ads']");
            foreach (var element in adsElements)
            {
                element.Remove();
            }

            // Get content of chapter in html format
            string content = contentElement.InnerHtml;

            var chapter = new Chapter();
            chapter.Title = "Chapter Title";
            chapter.Content = content;

            return chapter;
        }

        #region helper method
        /// <summary>
        /// CrawlNovels to crawl all novel in list format
        /// </summary>
        /// <param name="url"> Ex: int page will be replace by <page> url:https://truyenfull.vn/danh-sach/truyen-hot/trang-<page>/ </param>
        /// <returns>Tuple - First: Array of novel, Second: totalPage</returns>
        private async Task<Tuple<Novel[], int>> CrawlNovels(string url, int page = 1)
        {
            var web = new HtmlWeb();
            var document = await web.LoadFromWebAsync(url.Replace("<page>", page.ToString()));
            Regex regex = new Regex(@"\d+");

            // Get Pagination
            int totalPage = 1;
            var paginationElement = document.DocumentNode.QuerySelector("ul.pagination");
            if (paginationElement != null)
            {
                paginationElement.RemoveChild(paginationElement.LastChild);
                var lastChild = paginationElement.LastChild.QuerySelector("a");


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

            // Get novels
            // remove empty div in list-truyen
            var adsElements = document.DocumentNode.QuerySelectorAll("div.col-truyen-main div.list-truyen div[id]");
            foreach (var element in adsElements)
            {
                element.Remove();
            }
            var listNovel = new List<Novel>();
            var novelElements = document.DocumentNode.QuerySelectorAll(" div.col-truyen-main div.list-truyen div.row");
            foreach (var novelElement in novelElements)
            {
                Novel novel = new Novel();
                novel.Title = novelElement.QuerySelector("h3.truyen-title")?.InnerText;
                novel.Slug = novelElement.QuerySelector("h3.truyen-title a")?.Attributes["href"].Value.Replace(Url, "").Replace("/", "");

                var strAuthor = novelElement.QuerySelector("span.author")?.InnerText;
                var authorNames = strAuthor?.Split(',').Select(author => author.Trim()).ToArray();
                if (authorNames != null)
                {
                    List<Author> listAuthor = new List<Author>();
                    foreach (var name in authorNames)
                    {
                        var author = new Author();
                        author.Name = name;
                        listAuthor.Add(author);
                    }
                    novel.Authors = listAuthor.ToArray();
                }

                novel.Cover = novelElement.QuerySelector("div[data-image]")?.Attributes["data-image"].Value;

                listNovel.Add(novel);
            }

            return new Tuple<Novel[], int>(listNovel.ToArray(), totalPage);
        }
        #endregion
    }
}