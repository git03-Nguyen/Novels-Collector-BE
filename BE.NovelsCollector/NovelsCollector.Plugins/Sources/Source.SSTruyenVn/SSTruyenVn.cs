﻿using HtmlAgilityPack;
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
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Source.TruyenSSVn
{
    public class SSTruyenVn : ISourceFeature
    {
        private const string mainUrl = "https://sstruyen.vn/";
        public string SearchUrl => "https://sstruyen.vn/tim-truyen/<keyword>/";
        public string HotUrl => "https://sstruyen.vn/?lib=all&ct=&order=1&greater=0&lesser=1000000000&hot=hot&p=<page>";
        public string LatestUrl => "https://sstruyen.vn/?lib=all&ct=&order=8&greater=0&lesser=1000000000&p=<page>";
        public string CompletedUrl => "https://sstruyen.vn/?lib=all&ct=&order=4&greater=0&lesser=1000000000&full=full&p=<page>";
        public string AuthorUrl => "https://sstruyen.vn/tac-gia/<author-slug>/";
        public string CategoryUrl => "https://sstruyen.vn/the-loai/<category-slug>/trang-<page>/";
        public string NovelUrl => "https://sstruyen.vn/<novel-slug>/";
        public string ChapterUrl => "https://sstruyen.vn/<novel-slug>/<chapter-slug>";

        public SSTruyenVn() { }

        /// <summary>
        /// Crawl novels by search
        /// </summary>
        /// <param name="query"></param>
        /// <param name="page"></param>
        /// <returns>First: Novels, Second: total page</returns>
        public async Task<Tuple<Novel[]?, int>> CrawlSearch(string? query, int page = 1)
        {
            query = query.Replace(" ", "%20");
            var result = await CrawlNovels(SearchUrl.Replace("<keyword>", query), page);
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
            if (page != 1) return new Tuple<Novel[]?, int>(null, 0);

            var url = $"https://sstruyen.vn/ajax.php?search={query}";

            HttpClient httpClient = new HttpClient();
            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return new Tuple<Novel[]?, int>(null, 0);

            var jsonStr = await response.Content.ReadAsStringAsync();
            var items = JsonSerializer.Deserialize<List<ResponseItem>>(jsonStr);
            if (items == null || items.Count() == 0) return new Tuple<Novel[]?, int>(null, 0);

            List<Novel> listNovel = new List<Novel>();
            foreach (var item in items)
            {
                Novel novel = new Novel();
                novel.Title = item.Name;
                novel.Slug = item.Url.Replace("/", "");
                listNovel.Add(novel);
            }

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
            var result = await CrawlNovels(CategoryUrl.Replace("<category-slug>", categorySlug.ToString()), page);
            return result;
        }

        public async Task<Category[]> CrawlCategories()
        {
            List<Category> listCategory = new List<Category>();
            try
            {
                var document = await LoadFromWebAsync(mainUrl);

                var categoryElements = document.DocumentNode.QuerySelectorAll("div.section-header ul.sub-menu.sub-menu-cat a[href*='/danh-sach/']");
                foreach (var categoryElement in categoryElements)
                {
                    Category category = new Category();
                    category.Title = categoryElement.InnerText;
                    category.Slug = categoryElement.Attributes["href"].Value.Replace("/danh-sach/", "").Replace("/", "");

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

                novel.Title = document.DocumentNode.QuerySelector("div.title h1.title a")?.InnerText;

                novel.MaxRating = 10;

                // get rating
                var ratingElement = document.DocumentNode.QuerySelector("div.rate");
                if (ratingElement != null) novel.Rating = float.Parse(ratingElement.InnerText.Split("/").First());

                novel.Description = HtmlEntity.DeEntitize(document.DocumentNode.QuerySelector("div.content1 > p")?.InnerHtml);

                // get authors
                var authorElements = document.DocumentNode.QuerySelectorAll("div.content1 div.info a[href*='/tac-gia/']");
                List<Author> listAuthor = new List<Author>();
                foreach (var element in authorElements)
                {
                    var author = new Author();
                    author.Name = element.Attributes["title"].Value;
                    author.Slug = element.Attributes["href"].Value.Replace("/tac-gia/", "").Replace("/", "");
                    listAuthor.Add(author);
                }
                novel.Authors = listAuthor.ToArray();

                // get categories
                var genreElements = document.DocumentNode.QuerySelectorAll("div.content1 div.info a[href*='/the-loai/']");
                List<Category> listCategory = new List<Category>();
                foreach (var element in genreElements)
                {
                    var category = new Category();
                    category.Title = element.Attributes["title"].Value;
                    category.Slug = element.Attributes["href"].Value.Replace("/the-loai/", "").Replace("/", "");
                    listCategory.Add(category);
                }
                novel.Categories = listCategory.ToArray();

                string? status = document.DocumentNode.QuerySelector("div.content1 div.info span.status")?.InnerText.Trim();
                if (status == "Đang ra") novel.Status = EnumStatus.OnGoing;
                else if (status == "Full") novel.Status = EnumStatus.Completed;
                else novel.Status = EnumStatus.ComingSoon; // Defualt value

                // get cover
                var coverElement = document.DocumentNode.QuerySelector("div.book-list.story-details img.sstbcover");
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
            var listChapter = new List<Chapter>();

            try
            {
                var document = await LoadFromWebAsync($"https://sstruyen.vn/ajax.php?get_chapt&story_seo={novelSlug}&chapt=1");

                // return is
                // <select>
                //     <option value="/tao-tac/chuong-1/#j_content" selected>Chương 1</option>
                //     < option value = "/tao-tac/chuong-2/#j_content" > Chương 2 </ option >
                // ...
                // </select>

                var optionElements = document.DocumentNode.QuerySelectorAll("option");
                foreach (var optionElement in optionElements)
                {
                    var chapter = new Chapter();
                    chapter.NovelSlug = novelSlug;
                    chapter.Slug = optionElement.Attributes["value"].Value.Replace($"/{novelSlug}/", "").Replace("/#j_content", "");
                    chapter.Number = int.Parse(Regex.Match(optionElement.InnerText, @"\d+").Value);
                    chapter.Title = optionElement.InnerText;

                    listChapter.Add(chapter);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);

            }

            return listChapter.ToArray();
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
            string content = "";
            string title = "";
            int? number = null;
            try
            {
                // Get content of chapter in html format
                var contentElement = document.DocumentNode.QuerySelector("div.content.container1");
                content = contentElement?.InnerHtml;

                // Get title
                var titleElement = document.DocumentNode.QuerySelector("div.rv-chapt-title a").InnerText;
                var titleStrings = titleElement.Split(": ");
                title = titleStrings.Length == 1 ? titleStrings[0] : string.Join(": ", titleStrings.Skip(1));

                // Get number
                var match = Regex.Match(titleStrings[0], @"\d+");
                if (match.Success) number = int.Parse(match.Value);
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

            chapter.Source = "SSTruyenVn";
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
                var paginationElement = document.DocumentNode.QuerySelector("div.pagination.pc ul");
                if (paginationElement != null)
                {
                    var liElements = paginationElement.QuerySelectorAll("li");
                    foreach (var liElement in liElements)
                    {
                        var match = Regex.Match(liElement.InnerText, @"\d+");
                        if (match.Success && int.Parse(match.Value) > totalPage)
                        {
                            totalPage = int.Parse(match.Value);
                        }
                    }

                }

                // Get novels
                var novelElements = document.DocumentNode.QuerySelectorAll("div.table-list.pc tr");
                foreach (var novelElement in novelElements)
                {
                    Novel novel = new Novel();
                    novel.Title = novelElement.QuerySelector("td.info h3.rv-home-a-title a")?.InnerText;
                    novel.Slug = novelElement.QuerySelector("td.info h3.rv-home-a-title a")?.Attributes["href"].Value.Replace("/", "");

                    var authorElements = novelElement.QuerySelectorAll("a[itemprop='author']");
                    List<Author> listAuthor = new List<Author>();
                    foreach (var element in authorElements)
                    {
                        var author = new Author();
                        author.Name = element.InnerText.Trim();
                        listAuthor.Add(author);
                    }
                    novel.Authors = listAuthor.ToArray();

                    novel.Cover = novelElement.QuerySelector("td.image img")?.Attributes["src"].Value;

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
            using (HttpClient client = new HttpClient(new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.Brotli | DecompressionMethods.GZip | DecompressionMethods.Deflate }))
            {
                // Set up custom headers
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Add("Accept", "text/html");
                client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
                client.DefaultRequestHeaders.Add("Accept-Encoding", "br, gzip, deflate");
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
            return regex.Replace(temp, System.String.Empty).Replace('\u0111', 'd').Replace('\u0110', 'D');
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

        private class ResponseItem
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }
            [JsonPropertyName("name")]
            public string Name { get; set; }
            [JsonPropertyName("url")]
            public string Url { get; set; }
            [JsonPropertyName("seo")]
            public string Seo { get; set; }
            [JsonPropertyName("image_z")]
            public string Image { get; set; }
        }

        #endregion
    }
}
