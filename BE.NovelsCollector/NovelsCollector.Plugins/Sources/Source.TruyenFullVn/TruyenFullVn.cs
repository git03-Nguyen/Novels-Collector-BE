using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using NovelsCollector.Domain.Entities.Plugins.Sources;
using NovelsCollector.Domain.Resources.Authors;
using NovelsCollector.Domain.Resources.Categories;
using NovelsCollector.Domain.Resources.Chapters;
using NovelsCollector.Domain.Resources.Novels;
using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace Source.TruyenFullVn
{
    public class TruyenFullVn : ISourceFeature
    {
        private const string mainUrl = "https://truyenfull.vn/";
        public string SearchUrl => "https://truyenfull.vn/tim-kiem/?tukhoa=<keyword>&page=<page>";
        public string HotUrl => "https://truyenfull.vn/danh-sach/truyen-hot/trang-<page>/";
        public string LatestUrl => "https://truyenfull.vn/danh-sach/truyen-moi/trang-<page>/";
        public string CompletedUrl => "https://truyenfull.vn/danh-sach/truyen-full/trang-<page>/";
        public string AuthorUrl => "https://truyenfull.vn/tac-gia/<author-slug>/trang-<page>/";
        public string CategoryUrl => "https://truyenfull.vn/the-loai/<category-slug>/trang-<page>/";
        public string NovelUrl => "https://truyenfull.vn/<novel-slug>";
        public string ChapterUrl => "https://truyenfull.vn/<novel-slug>/<chapter-slug>";

        public TruyenFullVn() { }

        /// <summary>
        /// Crawl Detail All Novels. Note: Takes a lot of time and RAM
        /// </summary>
        /// <returns>Novel[]</returns>
        public async Task<Novel[]> CrawlDetailAllNovels()
        {
            var (novels, totalPage) = await CrawlNovels($"{mainUrl}danh-sach/truyen-moi/trang-<page>/");

            List<Novel> listNovel = new List<Novel>();
            for (int i = 1; i <= totalPage; i++)
            {
                var (tempNovels, tempTotalPage) = await CrawlNovels($"{mainUrl}danh-sach/truyen-moi/trang-<page>/", i);

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
        /// <param name="query"></param>
        /// <param name="page"></param>
        /// <returns>First: Novels, Second: total page</returns>
        public async Task<Tuple<Novel[]?, int>> CrawlSearch(string? query, int page = 1)
        {
            var reqStr = $"{mainUrl}tim-kiem/?tukhoa={query}";
            if (page > 1) reqStr += $"&page={page}";
            var result = await CrawlNovels(reqStr, page);
            return result;
        }

        /// <summary>
        /// Crawl quick search
        /// </summary>
        /// <param name="query"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        public async Task<Tuple<Novel[]?, int>> CrawlQuickSearch(string? query, int page = 1)
        {
            if (page != 1) return new Tuple<Novel[]?, int>(null, 1);

            var sQuery = query?.Replace(" ", "+");
            var url = $"https://truyenfull.vn/ajax.php?type=quick_search&str={sQuery}";

            HttpClient httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty");
            httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
            httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
            httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
            httpClient.DefaultRequestHeaders.Add("Referer", "https://truyenfull.vn/");
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36");
            httpClient.DefaultRequestHeaders.Add("Priority", "u = 1, i");

            var response = await httpClient.GetAsync(url);
            var htmlStr = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(htmlStr))
            {
                return await CrawlNovels(SearchUrl.Replace("<keyword>", query), page, true);
            }

            // Find every sub-string like https://truyenfull.vn/<slug>/ and get the slug
            var matches = Regex.Matches(htmlStr, @"https://truyenfull.vn/[^/]+/");
            List<string> slugs = new List<string>();
            foreach (Match match in matches)
            {
                slugs.Add(match.Value.Replace("https://truyenfull.vn/", "").Replace("/", ""));
            }
            if (slugs.Count == 0) return new Tuple<Novel[]?, int>(null, 1);

            // Find every sub-string like title="..." and get the title
            matches = Regex.Matches(htmlStr, @"title=""[^""]+""");
            List<string> titles = new List<string>();
            foreach (Match match in matches)
            {
                titles.Add(match.Value.Replace("title=\"", "").Replace("\"", ""));
            }

            // Add n-1 novel
            List<Novel> novels = new List<Novel>();
            for (int i = 0; i < slugs.Count - 1; i++)
            {
                novels.Add(new Novel
                {
                    Slug = slugs[i],
                    Title = titles[i]
                });
            }

            return new Tuple<Novel[]?, int>(novels.ToArray(), 1);
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
                var document = await LoadFromWebAsync(mainUrl);

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
                Console.WriteLine("An error occurred: " + ex.Message);
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
                var document = await LoadFromWebAsync($"{mainUrl}{novelSlug}/");
                novel.Id = int.Parse(document.DocumentNode.QuerySelector("input#truyen-id")?.Attributes["value"].Value);
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
                    author.Slug = element.Attributes["href"].Value.Replace($"{mainUrl}tac-gia", "").Replace("/", "");
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
                    category.Slug = element.Attributes["href"].Value.Replace($"{mainUrl}the-loai", "").Replace("/", "");
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
                Console.WriteLine("An error occurred: " + ex.Message);
            }

            return novel;
        }

        public async Task<Chapter[]?> CrawlListChapters(string novelSlug, string novelId)
        {
            var listChapters = new List<Chapter>();

            try
            {
                HtmlDocument? document = null;
                using (HttpClient httpClient = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.Brotli | DecompressionMethods.GZip | DecompressionMethods.Deflate }))
                {
                    httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
                    httpClient.DefaultRequestHeaders.Add("Accept", "*/*");

                    // If the novelId is not provided, crawl the novelId
                    if (novelSlug == novelId || novelId == null)
                    {
                        document = new HtmlDocument();
                        var html = await httpClient.GetStringAsync($"{mainUrl}{novelSlug}/");
                        document.LoadHtml(html);

                        var novelIdElement = document.DocumentNode.QuerySelector("input#truyen-id");
                        if (novelIdElement != null) novelId = novelIdElement.Attributes["value"].Value;
                        // If novelId is still null, return null
                        if (string.IsNullOrEmpty(novelId)) return null;
                    }

                    // Crawl list of chapters
                    //document = await LoadFromWebAsync($"https://truyenfull.vn/ajax.php?type=chapter_option&data={novelId}");
                    var url = $"https://truyenfull.vn/ajax.php?type=chapter_option&data={novelId}";
                    var response = await httpClient.GetAsync(url);
                    var htmlStr = await response.Content.ReadAsStringAsync();
                    document = new HtmlDocument();
                    document.LoadHtml(htmlStr);

                    // <select class="btn btn-success btn-chapter-nav form-control chapter_jump">
                    // < option value = "chuong-1" > Chương 1 </ option >
                    // < option value = "chuong-2" > Chương 2 </ option >
                    // ...

                    var chapterElements = document.DocumentNode.QuerySelectorAll("option");
                    foreach (var chapterElement in chapterElements)
                    {
                        var chapter = new Chapter();
                        chapter.Slug = chapterElement.Attributes["value"].Value;
                        chapter.Title = chapterElement.InnerText;
                        chapter.Number = int.Parse(Regex.Match(chapter.Title, @"\d+").Value);
                        listChapters.Add(chapter);
                    }

                }
            }

            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }

            return listChapters.ToArray();
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
                var document = await LoadFromWebAsync($"{mainUrl}{novelSlug}/{chapterSlug}/");

                string? content = null;
                string? title = null;
                int? number = null;

                var containerElement = document.DocumentNode.QuerySelector("#chapter-big-container");
                if (containerElement != null)
                {
                    // Get title of chatper
                    var titleElement = containerElement.QuerySelector(".chapter-title").InnerText;
                    var titleStrings = titleElement.Split(":");
                    title = titleStrings.Length == 1 ? titleStrings[0] : string.Join(": ", titleStrings.Skip(1));

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
                Console.WriteLine("An error occurred: " + ex.Message);

            }

            return chapter;
        }

        public async Task<Chapter?> GetChapterAddrByNumber(string novelSlug, int? novelId, int chapterNumber)
        {
            Chapter[]? chapters = await CrawlListChapters(novelSlug, novelId != null ? novelId.ToString() : null);
            if (chapters == null) return null;


            var chapter = chapters.FirstOrDefault(x => x.Number == chapterNumber);
            if (chapter == null) return null;

            chapter.Source = "TruyenFullVn";
            chapter.NovelSlug = novelSlug;
            return chapter;
        }

        #region helper method
        /// <summary>
        /// CrawlNovels to crawl all novel in list format
        /// </summary>
        /// <param name="url"> Ex: int page will be replace by <page> url:https://truyenfull.vn/danh-sach/truyen-hot/trang-<page>/ </param>
        /// <returns>Tuple - First: Array of novel, Second: totalPage</returns>
        private async Task<Tuple<Novel[], int>> CrawlNovels(string url, int page = 1, bool isQuickSearch = false)
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
                    novel.Slug = novelElement.QuerySelector("h3.truyen-title a")?.Attributes["href"].Value.Replace(mainUrl, "").Replace("/", "");

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
                if (!isQuickSearch)
                {
                    using (var client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
                        client.DefaultRequestHeaders.Add("Accept", "text/html");
                        client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");

                        // Limit the number of concurrent requests to a reasonable number
                        int maxConcurrency = Math.Min(listNovel.Count, 20); // e.g., limit to 20 concurrent requests
                        ServicePointManager.DefaultConnectionLimit = maxConcurrency;

                        var tasks = listNovel.Select(novel => crawlFullCovers(client, novel)).ToArray();

                        // Await the completion of all tasks
                        await Task.WhenAll(tasks);

                        Console.WriteLine($"Done all {listNovel.Count} covers");

                        // Reset the default connection limit
                        ServicePointManager.DefaultConnectionLimit = 10;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }

            return new Tuple<Novel[], int>(listNovel.ToArray(), totalPage);
        }

        private async Task crawlFullCovers(HttpClient client, Novel novel)
        {
            if (!novel.Cover.Contains("https://static.8cache.com/cover/"))
                novel.Cover = novel.Cover.Replace("w60-h85", "w215-h322").Replace("w180-h80", "w215-h322");

            try
            {
                var html = await client.GetStringAsync($"https://truyenfull.vn/{novel.Slug}/");
                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                var element = doc.DocumentNode.QuerySelector("div.books div.book img");
                if (element != null)
                {
                    //Console.WriteLine($"Done cover: {novel.Title}");
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
                    Console.WriteLine($"Not found full cover: {novel.Title} - {novel.Slug}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
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
                    Console.WriteLine("An error occurred: " + e.Message);
                }
            }

            return document;
        }
        #endregion
    }
}