﻿using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using log4net;
using NovelsCollector.SDK.Models;
using NovelsCollector.SDK.Plugins.SourcePlugins;
using System.Text.RegularExpressions;

namespace Source.TruyenSSVn
{
    public class TruyenSSVn: SourcePlugin, ISourcePlugin
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(TruyenSSVn));
        public string SearchUrl => "https://sstruyen.vn/tim-truyen/hello/";
        public string HotUrl => "https://sstruyen.vn/?lib=all&ct=&order=1&greater=0&lesser=1000000000&hot=hot&p=<page>";
        public string LatestUrl => "https://sstruyen.vn/?lib=all&ct=&order=8&greater=0&lesser=1000000000&p=<page>";
        public string CompletedUrl => "https://sstruyen.vn/?lib=all&ct=&order=4&greater=0&lesser=1000000000&full=full&p=<page>";
        public string AuthorUrl => "https://sstruyen.vn/tac-gia/<author-slug>/";
        public string CategoryUrl => "https://sstruyen.vn/the-loai/<category-slug>/";
        public string NovelUrl => "https://sstruyen.vn/<novel-slug>/";
        public string ChapterUrl => "https://sstruyen.vn/<novel-slug>/<chapter-slug>";

        public TruyenSSVn()
        {
            Url = "https://sstruyen.vn/";
            Name = "TruyenSSVn";
            Description = "This plugin is used to crawl novels from TruyenSSVn website";
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
            var result = await CrawlNovels(SearchUrl.Replace("<keyword>", keyword), page);
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
            var result = await CrawlNovels(AuthorUrl.Replace("<author-slug>", author.Slug.ToString()), page);
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
            var result = await CrawlNovels(CategoryUrl.Replace("<category-slug>", category.Slug.ToString()), page);
            return result;
        }

        public async Task<Category[]> CrawlCategories()
        {
            List<Category> listCategory = new List<Category>();
            try
            {
                var document = await LoadFromWebAsync(Url);

                var categoryElements = document.DocumentNode.QuerySelectorAll("div.section-header ul.sub-menu.sub-menu-cat a[href*='/danh-sach/']");
                foreach (var categoryElement in categoryElements)
                {
                    Category category = new Category();
                    category.Name = categoryElement.InnerText;
                    category.Slug = categoryElement.Attributes["href"].Value.Replace("/danh-sach/", "").Replace("/", "");

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

                novel.Title = document.DocumentNode.QuerySelector("div.title h1.title a")?.InnerText;

                novel.MaxRating = 10;

                // get rating
                var ratingElement = document.DocumentNode.QuerySelector("div.rate");
                if (ratingElement != null) novel.Rating = float.Parse(ratingElement.InnerText.Split("/").First());

                novel.Description = document.DocumentNode.QuerySelector("div.content1 > p")?.InnerHtml;

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
                    category.Name = element.Attributes["title"].Value;
                    category.Slug = element.Attributes["href"].Value.Replace("/the-loai/", "").Replace("/", "");
                    listCategory.Add(category);
                }
                novel.Categories = listCategory.ToArray();

                string? status = document.DocumentNode.QuerySelector("div.content1 div.info span.status")?.InnerText.Trim();
                if (status == "Đang ra") novel.Status = EnumStatus.OnGoing;
                else if (status == "Full") novel.Status = EnumStatus.Completed;
                else novel.Status = EnumStatus.ComingSoon; // Defualt value

                // get cover
                var coverElement = document.DocumentNode.QuerySelector("div.story-detail img.sstbcover");
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

        public async Task<Tuple<Chapter[]?, int>> CrawlListChapters(string novelSlug, int page = 1)
        {
            // Get the Id of novel
            var novel = new Novel();
            novel.Slug = novelSlug;
            var document = await LoadFromWebAsync(NovelUrl.Replace("<novel-slug>", novel.Slug));
            var idElement = document.DocumentNode.QuerySelector("meta[name='book_detail']");
            if (idElement != null) novel.Id = int.Parse(idElement.Attributes["content"].Value);

            // Get number of chapters: <p> tag having content: "Số chương"
            var pElements = document.DocumentNode.QuerySelectorAll("div.content1 div.info p");
            int totalChapter = 0;
            foreach (var pElement in pElements)
            {
                if (pElement.InnerText.Contains("Số chương"))
                {
                    totalChapter = int.Parse(pElement.QuerySelector("span.status").InnerText);
                }
            }

            var perPage = 32;
            // if page == -1, then it will return the last page
            if (page == -1)
            {
                page = (int)Math.Ceiling((double)totalChapter / perPage);
            }

            // list chapter
            List<Chapter> listChapter = new List<Chapter>();
            var url = $"https://sstruyen.vn/{novel.Slug}/trang-<page>/";
            document = await LoadFromWebAsync(url);
            var chapterElements = document.DocumentNode.QuerySelectorAll($"div.list-chap a[href*='/{novelSlug}/']");
            foreach (var element in chapterElements)
            {
                var chapter = new Chapter();
                chapter.Title = element.QuerySelector("a").Attributes["title"].Value;
                chapter.Slug = element.QuerySelector("a").Attributes["href"].Value.Replace($"/{novel.Slug}/", "").Replace("/", "");
                listChapter.Add(chapter);
            }

            return new Tuple<Chapter[]?, int>(listChapter.ToArray(), page);
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
            try
            {
                // Get content of chapter in html format
                var contentElement = document.DocumentNode.QuerySelector("div.content.container1");
                content = contentElement.InnerHtml;

                // Get title
                var titleElement = document.DocumentNode.QuerySelector("div.rv-chapt-title a");
                title = titleElement.InnerText;
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
                        author.Name = element.InnerText;
                        listAuthor.Add(author);
                    }
                    novel.Authors = listAuthor.ToArray();

                    novel.Cover = novelElement.QuerySelector("td.image img")?.Attributes["src"].Value;

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
        #endregion
    }
}