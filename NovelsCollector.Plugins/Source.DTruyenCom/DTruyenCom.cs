using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;
using System.Xml.Linq;
using System.Xml;
using System;
using log4net;
using NovelsCollector.SDK.Plugins.SourcePlugins;
using NovelsCollector.SDK.Models;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace DTruyenCom
{
    public class DTruyenCom : SourcePlugin, ISourcePlugin
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(DTruyenCom));
        public string SearchUrl => "https://dtruyen.com/searching/<keyword>/lastupdate/all/all/<page>/"; // keyword: using slug format
        public string HotUrl => "https://dtruyen.com/truyen-duoc-yeu-thich-nhat/<page>/";
        public string LatestUrl => "https://dtruyen.com/all/<page>/";
        public string CompletedUrl => "https://dtruyen.com/truyen-full/<page>/";
        public string AuthorUrl => "https://dtruyen.com/tac-gia/<author-slug>/<page>/";
        public string CategoryUrl => "https://dtruyen.com/<category-slug>/<page>/";
        public string NovelUrl => "https://dtruyen.com/<novel-slug>/";
        public string ChapterUrl => "https://dtruyen.com/<novel-slug>/<chapter-slug>";

        public DTruyenCom()
        {
            Url = "https://dtruyen.com/";
            Name = "DTruyenCom";
            Description = "This plugin is used to crawl novels from DTruyenCom website";
            Version = "1.0.0";
            Author = "Nguyen Tuan Dat";
            Enabled = true;
        }

        /// <summary>
        /// Crawl novels by search
        /// </summary>
        /// <param name="keyword"></param>
        /// <param name="page"></param>
        /// <returns>First: Novels, Second: total page</returns>
        public async Task<Tuple<Novel[]?, int>> CrawlSearch(string? keyword, int page = 1)
        {
            var slugKeyword = ConvertToSlug(keyword);
            var result = await CrawlNovels(SearchUrl.Replace("<keyword>", slugKeyword), page);
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
        public async Task<Tuple<Novel[], int>> CrawlByAuthor(Author author, int page = 1)
        {
            var result = await CrawlNovels(AuthorUrl.Replace("<author-slug>", author.Slug), page);
            return result;
        }

        /// <summary>
        /// Crawl novels which have the same category (using Category.Slug)
        /// </summary>
        /// <param name="category">Need: category.Slug</param>
        /// <param name="page"></param>
        /// <returns>First: Novels, Second: total page</returns>
        public async Task<Tuple<Novel[], int>> CrawlByCategory(Category category, int page = 1)
        {
            var result = await CrawlNovels(CategoryUrl.Replace("<category-slug>", category.Slug), page);
            return result;
        }

        public async Task<Category[]> CrawlCategories()
        {
            List<Category> listCategory = new List<Category>();
            try
            {
                var document = await LoadFromWebAsync(Url);
                var categoryElements = document.DocumentNode.QuerySelectorAll("div.categories.clearfix a[href*='https://dtruyen.com/']");
                foreach (var categoryElement in categoryElements)
                {
                    Category category = new Category();
                    category.Name = categoryElement.InnerText;
                    category.Slug = categoryElement.Attributes["href"].Value.Replace("https://dtruyen.com/", "").Replace("/", "");

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
            Novel novel = new Novel();
            novel.Slug = novelSlug;
            try
            {
                var document = await LoadFromWebAsync(NovelUrl.Replace("<novel-slug>", novel.Slug));

                novel.Title = document.DocumentNode.QuerySelector("meta[property='og:title']")?.Attributes["content"].Value;

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
                    author.Name = element.Attributes["title"].Value;
                    author.Slug = element.Attributes["href"].Value.Replace("https://dtruyen.com/tac-gia/", "").Replace("/", "");
                    listAuthor.Add(author);
                }
                novel.Authors = listAuthor.ToArray();

                // get categories
                var genreElements = document.DocumentNode.QuerySelectorAll("#story-detail p.story_categories a[itemprop='genre']");
                List<Category> listCategory = new List<Category>();
                foreach (var element in genreElements)
                {
                    var category = new Category();
                    category.Name = element.Attributes["title"].Value;
                    category.Slug = element.Attributes["href"].Value.Replace("https://dtruyen.com/", "").Replace("/", "");
                    listCategory.Add(category);
                }
                novel.Categories = listCategory.ToArray();

                string? status = document.DocumentNode.QuerySelector("#story-detail div.info:has(i.fa-star)")?.InnerText.Trim();
                if (status == "Đang cập nhật") novel.Status = EnumStatus.OnGoing;
                else if (status == "Hoàn Thành") novel.Status = EnumStatus.Completed;
                else novel.Status = EnumStatus.ComingSoon; // Defualt value

                novel.Cover = document.DocumentNode.QuerySelector("meta[property='og:image']")?.Attributes["content"].Value;
            }
            catch (Exception ex)
            {
                log.Error("An error occurred: ", ex);
            }

            return novel;
        }

        public async Task<Tuple<Chapter[]?, int>> CrawlListChapters(string novelSlug, int page = 1)
        {
            // Get the Id of novel
            var novel = new Novel();
            novel.Slug = novelSlug;

            var document = await LoadFromWebAsync(NovelUrl.Replace("<novel-slug>", novel.Slug));

            // Get number of chapters: <p> tag having content: "Số chương"
            var totalPage = 1;
            var paginationElement = document.DocumentNode.QuerySelector("ul.Pagination");
            if (paginationElement != null)
            {
                var aElements = paginationElement.QuerySelectorAll("li a");

                foreach (var element in aElements)
                {
                    var match = Regex.Match(element.InnerText, @"\d+");
                    if (match.Success && totalPage < int.Parse(match.Value))
                    {
                        totalPage = int.Parse(match.Value);
                    }
                }
            }

            // if page == -1, then it will return the last page
            if (page == -1)
            {
                page = totalPage;
            }

            // list chapter
            List<Chapter> listChapter = new List<Chapter>();
            var url = $"https://dtruyen.com/{novel.Slug}/{page}/";
            document = await LoadFromWebAsync(url);
            var chapterElements = document.DocumentNode.QuerySelectorAll($"#chapters ul.chapters li a");
            foreach (var element in chapterElements)
            {
                var chapter = new Chapter();
                chapter.Title = element.QuerySelector("a").Attributes["title"].Value;
                chapter.Slug = element.QuerySelector("a").Attributes["href"].Value.Replace($"https://dtruyen.com/{novel.Slug}/", "").Replace("/", "");
                listChapter.Add(chapter);
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
            var document = await LoadFromWebAsync(ChapterUrl.Replace("<novel-slug>", novelSlug).Replace("<chapter-slug>", chapterSlug));
            string? content = null;
            string? title = null;
            try
            {
                content = document.DocumentNode.QuerySelector("#chapter-content")?.InnerHtml;
                title = document.DocumentNode.QuerySelector("#chapter h2.chapter-title")?.InnerText;
            }
            catch (Exception ex)
            {
                log.Error("An error occurred: ", ex);
            }

            var chapter = new Chapter();
            chapter.Title = title;
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
                    novel.Title = novelElement.QuerySelector("div.info h3.title a")?.InnerText;
                    novel.Slug = novelElement.QuerySelector("div.info h3.title a")?.Attributes["href"].Value.Replace("https://dtruyen.com/", "").Replace("/", "");

                    var authorElements = novelElement.QuerySelectorAll("div.info p[itemprop='author']");
                    List<Author> listAuthor = new List<Author>();
                    foreach (var element in authorElements)
                    {
                        var author = new Author();
                        author.Name = element.InnerText;
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
                                novel.Cover = attribute.Value;
                            }
                        }
                    }

                    listNovel.Add(novel);
                }
            }
            catch (Exception ex)
            {
                log.Error("An error occurred: ", ex);
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
                    log.Error($"Request error: {e.Message}");
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
            input = HtmlEntity.DeEntitize(input);
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
    }
    #endregion
}

