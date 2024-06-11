using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using NovelsCollector.SDK.Models;
using NovelsCollector.SDK.Models.Plugins;
using NovelsCollector.SDK.Plugins.SourcePlugins;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Source.TruyenTangThuVienVn
{
    public class TruyenTangThuVienVn : SourcePlugin, ISourcePlugin
    {
        private const string mainUrl = "https://truyen.tangthuvien.vn/";
        public string SearchUrl => "https://truyen.tangthuvien.vn/ket-qua-tim-kiem?term=<keyword>&page=<page>";
        public string HotUrl => "https://truyen.tangthuvien.vn/tong-hop?rank=nm&time=m&page=<page>";
        public string LatestUrl => "https://truyen.tangthuvien.vn/tong-hop?tp=cv&page=<page>";
        public string CompletedUrl => "https://truyen.tangthuvien.vn/tong-hop?fns=ht&page=<page>";
        public string AuthorUrl => "https://truyen.tangthuvien.vn/tac-gia?author=<id>&page=<page>";
        public string CategoryUrl => "https://truyen.tangthuvien.vn/tong-hop?ctg=<id>&page=<page>";
        public string NovelUrl => "https://truyen.tangthuvien.vn/doc-truyen/<novel-slug>";
        public string ChapterUrl => "https://truyen.tangthuvien.vn/doc-truyen/<novel-slug>/<chapter-slug>";

        public TruyenTangThuVienVn() { }

        /// <summary>
        /// Crawl novels by search
        /// </summary>
        /// <param name="query"></param>
        /// <param name="page"></param>
        /// <returns>First: Novels, Second: total page</returns>
        public async Task<Tuple<Novel[]?, int>> CrawlSearch(string? query, int page = 1)
        {
            var result = await CrawlNovels(SearchUrl.Replace("<keyword>", query), page);
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
        /// Crawl novels which are written by this author (using Author.Id)
        /// </summary>
        /// <param name="author">Need: author.Id</param>
        /// <param name="page"></param>
        /// <returns>First: Novels, Second: total page</returns>
        public async Task<Tuple<Novel[], int>> CrawlByAuthor(string authorSlug, int page = 1)
        {
            //int authorId = await CrawlIdAuthor(authorSlug); 
            var result = await CrawlNovels(AuthorUrl.Replace("<id>", authorSlug), page);
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
            int categoryId = await CrawlIdCategory(categorySlug);
            var result = await CrawlNovels(CategoryUrl.Replace("id", categoryId.ToString()), page);
            return result;
        }

        public async Task<Category[]> CrawlCategories()
        {
            List<Category> listCategory = new List<Category>();
            try
            {
                var document = await LoadFromWebAsync(mainUrl);
                var categoryElements = document.DocumentNode.QuerySelectorAll("div.classify-list a[href*='https://truyen.tangthuvien.vn/the-loai/']");
                foreach (var categoryElement in categoryElements)
                {
                    Category category = new Category();

                    // Get name of category
                    var nameElement = categoryElement.QuerySelector("span[class='info'] i");
                    if (nameElement != null)
                    {
                        category.Title = HtmlEntity.DeEntitize(nameElement.InnerText)
                            .Replace("\r\n", "").Replace("\\\"", "\"").Replace("\\t", "  ");
                    }

                    category.Slug = categoryElement?.Attributes["href"].Value.Replace("https://truyen.tangthuvien.vn/the-loai/", "").Replace("/", "");

                    if (listCategory.Count(x => (x.Slug == category.Slug)) == 0)
                    {
                        listCategory.Add(category);
                    }
                }
            }
            catch (NullReferenceException ex)
            {
                Console.WriteLine("Null Reference Exception: " + ex.Message);
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

                // Get title of novel
                var titleElement = document.DocumentNode.QuerySelector("div.book-info h1");
                if (titleElement != null)
                {
                    novel.Title = HtmlEntity.DeEntitize(titleElement.InnerText)
                        .Replace("\r\n", "").Replace("\\\"", "\"").Replace("\\t", "  ");
                }

                // get id of novel
                var idElement = document.DocumentNode.QuerySelector("meta[name='book_detail']");
                if (idElement != null)
                {
                    novel.Id = int.Parse(idElement.Attributes["content"].Value);
                }

                novel.MaxRating = 5;

                // get rating
                var ratingElement = document.DocumentNode.QuerySelector("#myrate");
                if (ratingElement != null) novel.Rating = float.Parse(ratingElement.InnerText);

                // Get description of novel
                var descriptionElement = document.DocumentNode.QuerySelector("div.book-intro");
                if (descriptionElement != null)
                {
                    novel.Description = HtmlEntity.DeEntitize(descriptionElement.InnerHtml)
                        .Replace("\r\n", "").Replace("\\\"", "\"").Replace("\\t", "  ");
                }

                // get authors
                var authorElements = document.DocumentNode.QuerySelectorAll("div.book-information a[href*='https://truyen.tangthuvien.vn/tac-gia?author=']");
                List<Author> listAuthor = new List<Author>();
                foreach (var element in authorElements)
                {
                    var author = new Author();
                    author.Name = HtmlEntity.DeEntitize(element.InnerHtml);
                    author.Id = int.Parse(element.Attributes["href"].Value.Replace("https://truyen.tangthuvien.vn/tac-gia?author=", ""));
                    author.Slug = author.Id.ToString();
                    listAuthor.Add(author);
                }
                novel.Authors = listAuthor.ToArray();

                // get categories
                var genreElements = document.DocumentNode.QuerySelectorAll("div.book-information a[href*='https://truyen.tangthuvien.vn/the-loai/']");
                List<Category> listCategory = new List<Category>();
                foreach (var element in genreElements)
                {
                    var category = new Category();
                    category.Title = HtmlEntity.DeEntitize(element.InnerHtml);
                    category.Slug = element.Attributes["href"].Value.Replace("https://truyen.tangthuvien.vn/the-loai/", "");
                    //category.Id = await CrawlIdCategory(category.Slug);

                    listCategory.Add(category);
                }
                novel.Categories = listCategory.ToArray();

                string? status = document.DocumentNode.QuerySelector("div.book-information div.tag span")?.InnerText.Trim();
                if (!string.IsNullOrWhiteSpace(status)) status = HtmlEntity.DeEntitize(status);
                if (status == "Đang ra") novel.Status = EnumStatus.OnGoing;
                else if (status == "Hoàn thành") novel.Status = EnumStatus.Completed;
                else novel.Status = EnumStatus.ComingSoon; // Default value

                // get cover
                var coverElement = document.DocumentNode.QuerySelector("div.book-information div.book-img img");
                if (coverElement != null)
                {
                    novel.Cover = coverElement.Attributes["src"].Value;
                }
            }
            catch (NullReferenceException ex)
            {
                Console.WriteLine("Null Reference Exception: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }

            return novel;
        }

        public async Task<Tuple<Chapter[]?, int>> CrawlListChapters(string novelSlug, int page = -1)
        {
            List<Chapter> listChapter = new List<Chapter>();
            int totalPage = 1;
            try
            {
                // Get the Id of novel
                var novel = new Novel();
                novel.Slug = novelSlug;
                var document = await LoadFromWebAsync(NovelUrl.Replace("<novel-slug>", novel.Slug));
                var idElement = document.DocumentNode.QuerySelector("meta[name='book_detail']");
                if (idElement != null) novel.Id = int.Parse(idElement.Attributes["content"].Value);

                // Get number of chapters: <a> tag having content: "Danh sách chương (1036 chương)"
                var totalChapterText = document.DocumentNode.QuerySelector("a#j-bookCatalogPage.lang")?.InnerText;
                var matches = Regex.Match(totalChapterText, @"\d+").Value;
                var totalChapter = int.Parse(matches);
                var perPage = 75;

                // Get totalPage
                totalPage = (int)Math.Ceiling((double)totalChapter / perPage);

                // check page
                if (page == -1 || page > totalPage) page = totalPage;
                else if (page <= 0) page = 1;

                // list chapter
                var url = $"https://truyen.tangthuvien.vn/doc-truyen/page/{novel.Id}?page={page - 1}&limit={perPage}&web=1";
                document = await LoadFromWebAsync(url);
                var chapterElements = document.DocumentNode.QuerySelectorAll("a[href*='https://truyen.tangthuvien.vn/doc-truyen/']");
                foreach (var element in chapterElements)
                {
                    var chapter = new Chapter();
                    var titleStrings = HtmlEntity.DeEntitize(element.QuerySelector("a").Attributes["title"].Value).Split(":");
                    Match match = Regex.Match(titleStrings[0], @"\d+");
                    if (match.Success) chapter.Number = int.Parse(match.Value);
                    chapter.Title = titleStrings.Length > 1 ? titleStrings[1].Trim() : titleStrings[0].Trim();
                    chapter.Slug = element.QuerySelector("a").Attributes["href"].Value.Replace($"https://truyen.tangthuvien.vn/doc-truyen/{novel.Slug}/", "").Replace("/", "");
                    listChapter.Add(chapter);
                }
            }
            catch (NullReferenceException ex)
            {
                Console.WriteLine("Null Reference Exception: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
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
            string content = "";
            string title = "";
            int? number = null;

            try
            {
                var contentElement = document.DocumentNode.QuerySelector("div.box-chap");

                // Get content of chapter in html format
                if (contentElement != null)
                {
                    content = HtmlEntity.DeEntitize(contentElement.InnerHtml)
                        .Replace("\r\n", "<br/>").Replace("\\\"", "\"").Replace("\\t", "  ");
                }

                var titleElement = document.DocumentNode.QuerySelector("h1.truyen-title");

                // Get title of chapter
                if (contentElement != null)
                {
                    title = HtmlEntity.DeEntitize(titleElement.InnerText)
                        .Replace("\r\n", "<br/>").Replace("\\\"", "\"").Replace("\\t", "  ");
                }

                // Get number of chapter
                var match = Regex.Match(chapterSlug, @"\d+");
                if (match.Success) number = int.Parse(match.Value);

            }
            catch (NullReferenceException ex)
            {
                Console.WriteLine("Null Reference Exception: " + ex.Message);
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

        public async Task<Chapter?> GetChapterSlug(string novelSlug, int chapterNumber)
        {
            const int PER_PAGE = 75;

            // TODO
            var chapter = new Chapter();
            chapter.NovelSlug = novelSlug;
            chapter.Number = chapterNumber;
            chapter.Slug = "chuong-" + chapterNumber;
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
                    var lastLi = paginationElement.QuerySelectorAll("li").Last();
                    paginationElement.RemoveChild(lastLi);
                    lastLi = paginationElement.QuerySelectorAll("li").Last();

                    var aElement = paginationElement.QuerySelectorAll("li").Last().QuerySelector("a");
                    if (aElement == null)
                    {
                        MatchCollection matches = regex.Matches(lastLi.InnerText);
                        totalPage = int.Parse(matches[0].Value);
                    }
                    else
                    {
                        MatchCollection matches = regex.Matches(aElement.Attributes["href"].Value);
                        totalPage = int.Parse(matches[0].Value);
                    }
                }

                // Get novels
                var novelElements = document.DocumentNode.QuerySelectorAll("div.book-img-text ul li");
                foreach (var novelElement in novelElements)
                {
                    Novel novel = new Novel();
                    novel.Title = HtmlEntity.DeEntitize(novelElement.QuerySelector("div.book-mid-info h4 a")?.InnerHtml);
                    novel.Slug = novelElement.QuerySelector("div.book-mid-info h4 a")?.Attributes["href"].Value.Replace("https://truyen.tangthuvien.vn/doc-truyen/", "");

                    var authorElement = novelElement.QuerySelector("p.author a.name");
                    var id = authorElement?.Attributes["href"].Value.Replace("https://truyen.tangthuvien.vn/tac-gia?author=", "");
                    var strAuthor = HtmlEntity.DeEntitize(authorElement?.InnerHtml);
                    var authorNames = strAuthor?.Split(',').Select(author => author.Trim()).ToArray();
                    if (authorNames != null)
                    {
                        List<Author> listAuthor = new List<Author>();
                        foreach (var name in authorNames)
                        {
                            var author = new Author();
                            author.Slug = id;
                            author.Name = name;
                            listAuthor.Add(author);
                        }
                        novel.Authors = listAuthor.ToArray();
                    }

                    novel.Cover = novelElement.QuerySelector("div.book-img-box img")?.Attributes["src"].Value;

                    listNovel.Add(novel);
                }
            }
            catch (NullReferenceException ex)
            {
                Console.WriteLine("Null Reference Exception: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }

            return new Tuple<Novel[], int>(listNovel.ToArray(), totalPage);
        }

        private async Task<int> CrawlIdAuthor(string authorSlug)
        {

            int id = 0;

            var url = $"https://truyen.tangthuvien.vn/tim-kiem?term={authorSlug}";
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Accept", "*/*");
                client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36");

                var jsonStr = await client.GetStringAsync(url);
                var items = JsonSerializer.Deserialize<ResponseItem[]>(jsonStr);
                if (items == null || items.Count() == 0) return 0;

                foreach (var item in items)
                {
                    if (item.Type == "author")
                    {
                        id = item.Id;
                        break;
                    }
                }
            }

            return id;
        }

        private async Task<int> CrawlIdCategory(string categorySlug)
        {
            int id = 0;

            try
            {
                var document = await LoadFromWebAsync($"https://truyen.tangthuvien.vn/the-loai/{categorySlug}");
                var moreElement = document.DocumentNode.QuerySelector("a[href*='https://truyen.tangthuvien.vn/tong-hop?tp=cv&ctg=']");
                if (moreElement != null)
                {
                    id = int.Parse(moreElement.Attributes["href"].Value.Replace("https://truyen.tangthuvien.vn/tong-hop?tp=cv&ctg=", ""));
                }
            }
            catch (NullReferenceException ex)
            {
                Console.WriteLine("Null Reference Exception: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }

            return id;
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
        private class ResponseItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Url { get; set; }
            public string Type { get; set; }
            public int StoryType { get; set; }
        }

        #endregion
    }
}
