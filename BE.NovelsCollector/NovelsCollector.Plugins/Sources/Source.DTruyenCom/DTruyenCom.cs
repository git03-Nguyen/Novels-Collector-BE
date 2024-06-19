using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using NovelsCollector.Domain.Entities.Plugins.Sources;
using NovelsCollector.Domain.Resources.Authors;
using NovelsCollector.Domain.Resources.Categories;
using NovelsCollector.Domain.Resources.Chapters;
using NovelsCollector.Domain.Resources.Novels;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DTruyenCom
{
    public class DTruyenCom : ISourceFeature
    {
        private const string mainUrl = "https://dtruyen.com/";
        public string SearchUrl => "https://dtruyen.com/searching/<keyword>/lastupdate/all/all/<page>/"; // keyword: using slug format
        public string HotUrl => "https://dtruyen.com/truyen-duoc-yeu-thich-nhat/<page>/";
        public string LatestUrl => "https://dtruyen.com/all/<page>/";
        public string CompletedUrl => "https://dtruyen.com/truyen-full/<page>/";
        public string AuthorUrl => "https://dtruyen.com/tac-gia/<author-slug>/<page>/";
        public string CategoryUrl => "https://dtruyen.com/<category-slug>/<page>/";
        public string NovelUrl => "https://dtruyen.com/<novel-slug>/";
        public string ChapterUrl => "https://dtruyen.com/<novel-slug>/<chapter-slug>";

        public DTruyenCom() { }

        /// <summary>
        /// Crawl novels by search
        /// </summary>
        /// <param name="query"></param>
        /// <param name="page"></param>
        /// <returns>First: Novels, Second: total page</returns>
        public async Task<Tuple<Novel[]?, int>> CrawlSearch(string? query, int page = 1)
        {
            var slugKeyword = ConvertToSlug(query);
            var result = await CrawlNovels(SearchUrl.Replace("<keyword>", slugKeyword), page);
            return result;
        }

        /// <summary>
        /// Crawl novels by quick search
        /// </summary>
        /// <param name="query"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        public async Task<Tuple<Novel[]?, int>> CrawlQuickSearch(string? query, int page = 1)
        {
            if (page != 1) return new Tuple<Novel[]?, int>(null, 1);

            var url = $"https://dtruyen.com/ajax/search/";
            var httpClient = new HttpClient();

            /*
                fetch("https://dtruyen.com/ajax/search/", {
                    method: "POST",
                    headers: {'content-type': 'multipart/form-data; boundary=----WebKitFormBoundarytyRBCFricHg9hgBq'},
                    body: `------WebKitFormBoundarytyRBCFricHg9hgBq
                        Content-Disposition: form-data; name="key"
                        
                        tao tac
                        ------WebKitFormBoundarytyRBCFricHg9hgBq--`
                    });  
             */

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent($"------WebKitFormBoundarytyRBCFricHg9hgBq\nContent-Disposition: form-data; name=\"key\"\n\n{query}\n------WebKitFormBoundarytyRBCFricHg9hgBq--", Encoding.UTF8);
            request.Content.Headers.Remove("Content-Type");
            var x = request.Content.Headers.TryAddWithoutValidation("Content-Type", "multipart/form-data; boundary=----WebKitFormBoundarytyRBCFricHg9hgBq");
            var response = await httpClient.SendAsync(request);

            // deserialize json as Dictionary<string, List<Dictionary<string, string>>>
            var jsonString = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<Dictionary<string, List<Dictionary<string, object>>>>(jsonString);

            List<Novel> listNovel = new List<Novel>();
            foreach (var story in json["stories"])
            {
                Novel novel = new Novel()
                {
                    Id = int.Parse(story["ID"].ToString()),
                    Title = story["Name"].ToString(),
                    Slug = story["Key"].ToString(),
                    Authors = [new Author() { Name = story["AuthorName"].ToString() }]
                };

                listNovel.Add(novel);
            }

            if (listNovel.Count == 0) return new Tuple<Novel[]?, int>(null, 1);

            return new Tuple<Novel[]?, int>(listNovel.ToArray(), 1);

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
                var categoryElements = document.DocumentNode.QuerySelectorAll("div.categories.clearfix a[href*='https://dtruyen.com/']");
                foreach (var categoryElement in categoryElements)
                {
                    Category category = new Category();
                    category.Title = categoryElement.InnerText;
                    category.Slug = categoryElement.Attributes["href"].Value.Replace("https://dtruyen.com/", "").Replace("/", "");

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
            Novel novel = new Novel();
            novel.Slug = novelSlug;
            try
            {
                var document = await LoadFromWebAsync(NovelUrl.Replace("<novel-slug>", novel.Slug));

                novel.Id = int.Parse(document.DocumentNode.QuerySelector("input#storyID").Attributes["value"].Value);
                novel.Title = document.DocumentNode.QuerySelector("div#story-detail h1.title").InnerText;

                novel.MaxRating = 10;

                // get rating
                var ratingElement = document.DocumentNode.QuerySelector("div.rate strong span");
                if (ratingElement != null) novel.Rating = float.Parse(ratingElement.InnerText);

                novel.Description = document.DocumentNode.QuerySelector("#story-detail div.description")?.InnerHtml;

                // get authors
                var authorElements = document.DocumentNode.QuerySelectorAll("#story-detail a[itemprop='author']");
                List<Author> listAuthor = new List<Author>();
                foreach (var element in authorElements)
                {
                    var author = new Author();
                    author.Name = element.Attributes["title"].Value.Trim();
                    author.Slug = element.Attributes["href"].Value.Replace("https://dtruyen.com/tac-gia/", "").Replace("/", "").Trim();
                    listAuthor.Add(author);
                }
                novel.Authors = listAuthor.ToArray();

                // get categories
                var genreElements = document.DocumentNode.QuerySelectorAll("#story-detail p.story_categories a[itemprop='genre']");
                List<Category> listCategory = new List<Category>();
                foreach (var element in genreElements)
                {
                    var category = new Category();
                    category.Title = element.Attributes["title"].Value;
                    category.Slug = element.Attributes["href"].Value.Replace("https://dtruyen.com/", "").Replace("/", "");
                    listCategory.Add(category);
                }
                novel.Categories = listCategory.ToArray();

                var statusElement = document.DocumentNode.QuerySelector("#story-detail div.infos").LastChild.PreviousSiblingElement().PreviousSiblingElement();
                if (statusElement != null)
                {
                    var status = statusElement.InnerText.Trim();
                    if (status == "Đang cập nhật") novel.Status = EnumStatus.OnGoing;
                    else if (status == "Hoàn Thành") novel.Status = EnumStatus.Completed;
                    else novel.Status = EnumStatus.ComingSoon; // Defualt value
                }

                var coverElement = document.DocumentNode.QuerySelector("div#story-detail img.cover");
                if (coverElement != null)
                {
                    foreach (var attribute in coverElement.Attributes)
                    {
                        if (attribute.Value.Contains("https://"))
                        {
                            novel.Cover = attribute.Value;
                            break;
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
            List<Chapter> listChapters = new List<Chapter>();

            try
            {
                var httpClient = new HttpClient(new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.Brotli | DecompressionMethods.GZip | DecompressionMethods.Deflate });
                httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "br, gzip, deflate");
                //  GET https://dtruyen.com/ajax/chapters?storyID={novel.Id ?? novel.Slug}
                var response = await httpClient.GetAsync($"https://dtruyen.com/ajax/chapters?storyID={novelId}");
                var jsonString = await response.Content.ReadAsStringAsync();
                // {"status":1,"chapters":[{"url":"bien-co_3621762.html","no":"Ch\u01b0\u01a1ng 1"},{"url":"bay-gio-lay-vi-tien-chay-ngay_3621763.html","no":"Ch\u01b0\u01a1ng 2"}, ... ]}

                var json = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);
                var list = (JsonElement)json["chapters"];

                for (int i = 0; i < list.GetArrayLength(); i++)
                {
                    var chapter = list[i];
                    Chapter c = new Chapter();
                    c.Title = chapter.GetProperty("no").GetString();
                    c.Slug = chapter.GetProperty("url").GetString();
                    var match = Regex.Match(c.Title, @"\d+");
                    if (match.Success) c.Number = int.Parse(match.Value);
                    listChapters.Add(c);
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
            var document = await LoadFromWebAsync(ChapterUrl.Replace("<novel-slug>", novelSlug).Replace("<chapter-slug>", chapterSlug));
            string? content = null;
            string? title = null;
            int? number = null;
            try
            {
                content = document.DocumentNode.QuerySelector("#chapter-content")?.InnerHtml;
                var titleStrings = (document.DocumentNode.QuerySelector("#chapter h2.chapter-title")?.InnerText).Split(": ");
                var match = Regex.Match(titleStrings[0], @"\d+");
                if (match.Success) number = int.Parse(match.Value);
                if (titleStrings.Length == 1) title = titleStrings[0];
                else title = string.Join(": ", titleStrings.Skip(1));
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }

            var chapter = new Chapter();
            chapter.Number = number;
            chapter.Title = title;
            chapter.Content = content;
            return chapter;
        }

        public async Task<Chapter?> GetChapterAddrByNumber(string novelSlug, int? novelId, int chapterNumber)
        {
            Chapter[]? chapters = await CrawlListChapters(novelSlug, novelId != null ? novelId.ToString() : null);
            if (chapters == null) return null;


            var chapter = chapters.FirstOrDefault(x => x.Number == chapterNumber);
            if (chapter == null) return null;

            chapter.Source = "DTruyenCom";
            chapter.NovelSlug = novelSlug;
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
            int totalPage = 1;
            var listNovel = new List<Novel>();

            try
            {
                var document = await LoadFromWebAsync(url.Replace("<page>", page.ToString()));
                Regex regex = new Regex(@"\d+");

                // Get Pagination
                var paginationElement = document.DocumentNode.QuerySelector("ul.pagination");
                if (paginationElement != null)
                {
                    var aElements = paginationElement.QuerySelectorAll("li a");
                    foreach (var element in aElements)
                    {
                        var match = Regex.Match(element.InnerText, @"\d+");
                        if (match.Success && int.Parse(match.Value) > totalPage)
                        {
                            totalPage = int.Parse(match.Value);
                        }
                    }

                }

                // remove promote
                var promoteElements = document.DocumentNode.QuerySelectorAll(".promote-vip");
                foreach (var promoteElement in promoteElements)
                {
                    promoteElement.Remove();
                }

                // Get novels
                var novelElements = document.DocumentNode.QuerySelectorAll("div.list-stories ul li");
                foreach (var novelElement in novelElements)
                {
                    Novel novel = new Novel();
                    novel.Id = int.Parse(novelElement.Attributes["data-id"].Value);
                    novel.Title = novelElement.QuerySelector("div.info h3.title a")?.InnerText;
                    novel.Slug = novelElement.QuerySelector("div.info h3.title a")?.Attributes["href"].Value.Replace("https://dtruyen.com/", "").Replace("/", "");

                    var authorElements = novelElement.QuerySelectorAll("div.info p[itemprop='author']");
                    List<Author> listAuthor = new List<Author>();
                    foreach (var element in authorElements)
                    {
                        var author = new Author();
                        author.Name = element.InnerText.Trim();
                        listAuthor.Add(author);
                    }
                    novel.Authors = listAuthor.ToArray();

                    // get novel.cover
                    var coverElement = novelElement.QuerySelector("img");
                    if (coverElement != null)
                    {
                        foreach (var attribute in coverElement.Attributes)
                        {
                            if (attribute.Value.Contains("https://"))
                            {
                                novel.Cover = attribute.Value.Replace("/images/small/", "/images/medium/");
                                break;
                            }
                        }
                    }

                    listNovel.Add(novel);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }

            return new Tuple<Novel[], int>(listNovel.ToArray(), totalPage);
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
        private string convertToUnSign(string s)
        {
            Regex regex = new Regex("\\p{IsCombiningDiacriticalMarks}+");
            string temp = s.Normalize(NormalizationForm.FormD);
            return regex.Replace(temp, String.Empty).Replace('\u0111', 'd').Replace('\u0110', 'D');
        }
        private string ConvertToSlug(string input)
        {
            input = convertToUnSign(input);

            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            // Normalize the string to decompose the characters
            string normalizedString = input.Normalize(NormalizationForm.FormD);

            // Remove diacritics
            StringBuilder stringBuilder = new StringBuilder();
            foreach (char c in normalizedString)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            // Convert to lowercase
            string withoutDiacritics = stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();

            // Remove special characters
            string slug = Regex.Replace(withoutDiacritics, @"[^a-z0-9\s-]", " ");

            // Replace spaces with hyphens
            slug = Regex.Replace(slug, @"\s+", "-").Trim('-');

            // Trim hyphens
            slug = slug.Trim('-');

            return slug;
        }


        #endregion
    }
}

