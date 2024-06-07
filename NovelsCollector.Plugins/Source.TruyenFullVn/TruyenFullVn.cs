using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using log4net;
using log4net.Core;
using NovelsCollector.SDK.Models;
using NovelsCollector.SDK.Plugins.SourcePlugins;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Source.TruyenFullVn
{
    public class TruyenFullVn : SourcePlugin, ISourcePlugin
    {

        private static readonly ILog log = LogManager.GetLogger(typeof(TruyenFullVn));

        public string SearchUrl => "https://truyenfull.vn/tim-kiem/?tukhoa=<keyword>&page=<page>";
        public string HotUrl => "https://truyenfull.vn/danh-sach/truyen-hot/trang-<page>/";
        public string LatestUrl => "https://truyenfull.vn/danh-sach/truyen-moi/trang-<page>/";
        public string CompletedUrl => "https://truyenfull.vn/danh-sach/truyen-full/trang-<page>/";
        public string AuthorUrl => "https://truyenfull.vn/tac-gia/<author-slug>/trang-<page>/";
        public string CategoryUrl => "https://truyenfull.vn/the-loai/<category-slug>/trang-<page>/";
        public string NovelUrl => "https://truyenfull.vn/<novel-slug>";
        public string ChapterUrl => "https://truyenfull.vn/<novel-slug>/<chapter-slug>";

        public TruyenFullVn()
        {
            Url = "https://truyenfull.vn/";
            Name = "TruyenFullVn";
            Description = "This plugin is used to crawl novels from truyenfull.vn";
            Version = "1.0.0";
            Author = "Nguyen Tuan Dat";
            Enabled = true;
        }

        /// <summary>
        /// Crawl Detail All Novels. Note: Takes a lot of time and RAM
        /// </summary>
        /// <returns>Novel[]</returns>
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
            var reqStr = $"{Url}tim-kiem/?tukhoa={keyword}";
            if (page > 1) reqStr += $"&page={page}";
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
            var result = await CrawlNovels(HotUrl, page);
            return result;
        }

        /// <summary>
        /// Crawl latest novles
        /// </summary>
        /// <param name="page"></param>
        /// <returns>First: Novels, Second: total page</returns>
        public async Task<Tuple<Novel[], int>> CrawlLatest(int page = 1)
        {
            var result = await CrawlNovels(LatestUrl, page);
            return result;
        }

        /// <summary>
        /// Crawl completed novels
        /// </summary>
        /// <param name="page"></param>
        /// <returns>First: Novels, Second: total page</returns>
        public async Task<Tuple<Novel[], int>> CrawlCompleted(int page = 1)
        {
            var result = await CrawlNovels(CompletedUrl, page);
            return result;
        }

        /// <summary>
        /// Crawl novels which are written by this author (using Author.Slug)
        /// </summary>
        /// <param name="author">Need: author.Slug</param>
        /// <param name="page"></param>
        /// <returns>First: Novels, Second: total page</returns>
        public async Task<Tuple<Novel[], int>> CrawlByAuthor(string authorSlug, int page = 1)
        {
            var result = await CrawlNovels(AuthorUrl.Replace("<author-slug>", authorSlug), page);
            return result;
        }

        /// <summary>
        /// Crawl novels which have the same category (using Category.Slug)
        /// </summary>
        /// <param name="category">Need: category.Slug</param>
        /// <param name="page"></param>
        /// <returns>First: Novels, Second: total page</returns>
        public async Task<Tuple<Novel[], int>> CrawlByCategory(string categorySlug, int page = 1)
        {
            var result = await CrawlNovels(CategoryUrl.Replace("<category-slug>", categorySlug), page);
            return result;
        }

        public async Task<Category[]> CrawlCategories()
        {
            List<Category> listCategory = new List<Category>();

            try
            {
                var document = await LoadFromWebAsync(Url);

                var categoryElements = document.DocumentNode.QuerySelectorAll("ul.navbar-nav a[href*='https://truyenfull.vn/the-loai/']");
                foreach (var categoryElement in categoryElements)
                {
                    Category category = new Category();
                    category.Title = categoryElement?.InnerText;
                    category.Slug = categoryElement?.Attributes["href"].Value.Replace("https://truyenfull.vn/the-loai/", "").Replace("/", "");

                    if (listCategory.Count(x => (x.Slug == category.Slug)) == 0)
                    {
                        listCategory.Add(category);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("An error occurred: ", ex);
            }

            return listCategory.ToArray();
        }

        /// <summary>
        /// Crawl detail information of novel (using Novel.Slug)
        /// </summary>
        /// <param name="novel">Need: novel.Slug</param>
        /// <returns>Novel</returns>
        public async Task<Novel?> CrawlDetail(string novelSlug)
        {
            var novel = new Novel();

            try
            {
                var document = await LoadFromWebAsync($"{Url}{novelSlug}/");

                novel.Slug = novelSlug;
                novel.Title = document.DocumentNode.QuerySelector("h3.title")?.InnerText;
                novel.MaxRating = 10;
                novel.Description = document.DocumentNode.QuerySelector("div[itemprop='description']")?.InnerText;

                // get rating value
                var ratingElement = document.DocumentNode.QuerySelector("span[itemprop='ratingValue']");
                if (ratingElement != null) novel.Rating = float.Parse(ratingElement.InnerText);

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
                    category.Title = element.Attributes["title"].Value;
                    category.Slug = element.Attributes["href"].Value.Replace($"{Url}the-loai", "").Replace("/", "");
                    listCategory.Add(category);
                }
                novel.Categories = listCategory.ToArray();

                // get status
                string? status = document.DocumentNode.QuerySelector("span.text-success")?.InnerText.Trim();
                if (status == "Đang ra") novel.Status = EnumStatus.OnGoing;
                else if (status == "Full") novel.Status = EnumStatus.Completed;
                else novel.Status = EnumStatus.ComingSoon; // Defualt value

                // get cover
                var coverElement = document.DocumentNode.QuerySelector("div.books img");
                if (coverElement != null)
                {
                    foreach (var attribute in coverElement.Attributes)
                    {
                        if (attribute.Value.Contains("https://"))
                        {
                            novel.Cover = attribute.Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("An error occurred: ", ex);
            }

            return novel;
        }

        public async Task<Tuple<Chapter[]?, int>> CrawlListChapters(string novelSlug, int page = -1)
        {
            var listChapter = new List<Chapter>();
            int totalPage = 1;
            try
            {
                var document = await LoadFromWebAsync($"{Url}{novelSlug}/");

                totalPage = int.Parse(document.DocumentNode.QuerySelector("input#total-page")?.Attributes["value"].Value);
                string? truyenId = document.DocumentNode.QuerySelector("input#truyen-id")?.Attributes["value"].Value;
                string? truyenAscii = document.DocumentNode.QuerySelector("input#truyen-ascii")?.Attributes["value"].Value;
                string? truyenName = document.DocumentNode.QuerySelector("h3.title")?.InnerText;

                // check page
                if (page == -1 || page > totalPage) page = totalPage;
                else if (page <= 0) page = 1;


                var url = $"{Url}ajax.php?type=list_chapter&tid={truyenId}&tascii={truyenAscii}&tname={truyenName}&page={page}&totalp={totalPage}";

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Accept", "*/*");
                    client.DefaultRequestHeaders.Add("Referer", $"{Url}{novelSlug}/trang-1");
                    client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36");

                    var jsonStr = await client.GetStringAsync(url);
                    var json = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonStr);
                    if (json == null) return new Tuple<Chapter[]?, int>(null, totalPage);
                    document.LoadHtml(json["chap_list"]);
                    var nodes = document.QuerySelectorAll("a");
                    foreach (var node in nodes)
                    {
                        var chapter = new Chapter();
                        chapter.Title = node.Attributes["title"].Value;
                        chapter.Slug = node.Attributes["href"].Value.Replace($"https://truyenfull.vn/{novelSlug}/", "").Replace("/", "");
                        listChapter.Add(chapter);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("An error occurred: ", ex);
            }

            return new Tuple<Chapter[]?, int>(listChapter.ToArray(), totalPage);
        }

        /// <summary>
        /// Crawl content of chapter (Using Novel.Slug and Chapter.Slug)
        /// </summary>
        /// <param name="novel">Need: novel.Slug</param>
        /// <param name="chapter"><Need: chapter.Slug/param>
        /// <returns>string</returns>
        public async Task<Chapter?> CrawlChapter(string novelSlug, string chapterSlug)
        {
            var chapter = new Chapter();

            try
            {
                var document = await LoadFromWebAsync($"{Url}{novelSlug}/{chapterSlug}/");

                string? content = null;
                string? title = null;
                int? number = null;

                var containerElement = document.DocumentNode.QuerySelector("#chapter-big-container");
                if (containerElement != null)
                {
                    // Get title of chatper
                    var titleElement = containerElement.QuerySelector(".chapter-title").InnerText;
                    var titleStrings = titleElement.Split(":");
                    if (titleStrings.Length > 1)
                    {
                        title = titleStrings[1].Trim();
                    }

                    // Get number of chapter
                    var match = Regex.Match(titleStrings[0], @"\d+");
                    if (match.Success) number = int.Parse(match.Value);


                    var contentElement = containerElement.QuerySelector("#chapter-c");

                    // Remove all ads
                    var adsElements = contentElement.QuerySelectorAll("div[class*='ads']");
                    foreach (var element in adsElements)
                    {
                        element.Remove();
                    }

                    // Get content of chapter in html format
                    content = contentElement.InnerHtml;
                }

                chapter.Number = number;
                chapter.Title = title;
                chapter.Content = content;
            }
            catch (Exception ex)
            {
                log.Error("An error occurred: ", ex);

            }

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
            var listNovel = new List<Novel>();
            int totalPage = 1;

            try
            {
                var document = await LoadFromWebAsync(url.Replace("<page>", page.ToString()));
                Regex regex = new Regex(@"\d+");

                // if document is empty text
                if (document.DocumentNode.InnerText == "")
                {
                    document = await LoadFromWebAsync(url.Replace(" ", "+"));
                }

                // Get Pagination
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

                var novelElements = document.DocumentNode.QuerySelectorAll("div.col-truyen-main div.list-truyen div.row");

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

                // Process for cropped image

                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Add("Accept", "text/html");
                client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");

                var listTask = new List<Task>();

                foreach (var novel in listNovel)
                {
                    listTask.Add(crawlFullCovers(client, novel));
                }

                await Task.WhenAll(listTask);
                Console.WriteLine("Done all full cover");
            }
            catch (Exception ex)
            {
                log.Error("An error occurred: ", ex);
            }

            return new Tuple<Novel[], int>(listNovel.ToArray(), totalPage);
        }

        private async Task crawlFullCovers(HttpClient client, Novel novel)
        {
            if (!novel.Cover.Contains("https://static.8cache.com/cover/")) 
                novel.Cover = novel.Cover.Replace("w60-h85", "w215-h322").Replace("w180-h80", "w215-h322");

            try
            {
                Console.WriteLine($"Crawl: {novel.Title}");
                var html = await client.GetStringAsync($"https://truyenfull.vn/{novel.Slug}/");
                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                var element = doc.DocumentNode.QuerySelector("div.books div.book img");
                if (element != null)
                {
                    Console.WriteLine($"Done cover: {novel.Title}");
                    if (element.Attributes.Contains("data-cfsrc"))
                    {
                        novel.Cover = element.Attributes["data-cfsrc"].Value;
                    }
                    else
                    {
                        novel.Cover = element.Attributes["src"].Value;
                    }
                }
                else
                {
                    Console.WriteLine($"Not found cover: {novel.Title} - {novel.Slug}");
                    Console.WriteLine(html);
                }
            }
            catch (Exception ex)
            {
                log.Error("An error occurred: ", ex);
            }

        }

        private async Task<HtmlDocument> LoadFromWebAsync(string url)
        {
            var document = new HtmlDocument();
            // Create a new instance of HttpClient
            using (HttpClient client = new HttpClient())
            {
                // Set up custom headers
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Add("Accept", "text/html");
                client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
                // Add more headers as needed

                try
                {
                    // Make the GET request
                    HttpResponseMessage response = await client.GetAsync(url);

                    // Ensure the request was successful
                    response.EnsureSuccessStatusCode();

                    // Read the response content
                    string responseBody = await response.Content.ReadAsStringAsync();

                    // Load html
                    document.LoadHtml(responseBody);
                }
                catch (HttpRequestException e)
                {
                    // Handle any errors
                    log.Error($"Request error: {e.Message}");
                }
            }

            return document;
        }
        #endregion
    }
}