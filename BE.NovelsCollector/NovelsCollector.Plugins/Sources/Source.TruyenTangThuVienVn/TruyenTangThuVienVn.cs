﻿using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using NovelsCollector.Domain.Entities.Plugins.Sources;
using NovelsCollector.Domain.Resources.Authors;
using NovelsCollector.Domain.Resources.Categories;
using NovelsCollector.Domain.Resources.Chapters;
using NovelsCollector.Domain.Resources.Novels;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Source.TruyenTangThuVienVn
{
    public class TruyenTangThuVienVn : ISourceFeature
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
        /// Crawl quick search
        /// </summary>
        /// <param name="query"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        public async Task<Tuple<Novel[]?, int>> CrawlQuickSearch(string? query, int page = 1)
        {
            if (page != 1) return new Tuple<Novel[]?, int>(null, 0);

            var sQuery = query.Replace(" ", "+");
            var url = $"https://truyen.tangthuvien.vn/tim-kiem?term={sQuery}";

            HttpClient httpClient = new HttpClient();
            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return new Tuple<Novel[]?, int>(null, 0);

            var jsonStr = await response.Content.ReadAsStringAsync();
            var items = JsonSerializer.Deserialize<List<ResponseItem>>(jsonStr);
            if (items == null || items.Count() == 0) return new Tuple<Novel[]?, int>(null, 0);

            List<Novel> listNovel = new List<Novel>();
            foreach (var item in items)
            {
                if (item.Type == "story")
                {
                    Novel novel = new Novel();
                    novel.Id = item.Id;
                    novel.Title = item.Name;
                    novel.Slug = item.Url;
                    listNovel.Add(novel);
                }
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
            var result = await CrawlNovels(CategoryUrl.Replace("<id>", categoryId.ToString()), page);
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

        public async Task<Chapter[]?> CrawlListChapters(string novelSlug, string novelId)
        {
            List<Chapter> listChapter = new List<Chapter>();

            try
            {
                HtmlDocument? document = null;
                using (HttpClient httpClient = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.Brotli | DecompressionMethods.GZip | DecompressionMethods.Deflate }))
                {
                    httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");

                    // If the novelId is not provided, crawl the novelId
                    if (novelId == novelSlug || novelId == null)
                    {
                        //document = await LoadFromWebAsync($"https://truyen.tangthuvien.vn/doc-truyen/{novelSlug}");
                        document = new HtmlDocument();
                        var res = await httpClient.GetAsync($"https://truyen.tangthuvien.vn/doc-truyen/{novelSlug}");
                        if (!res.IsSuccessStatusCode) return null;
                        var resBody = await res.Content.ReadAsStringAsync();
                        document.LoadHtml(resBody);
                        var idElement = document.DocumentNode.QuerySelector("input#story_id_hidden");
                        if (idElement != null) novelId = idElement.Attributes["value"].Value;
                        else return null;
                    }

                    // Get list of chapters
                    var response = await httpClient.GetAsync($"https://truyen.tangthuvien.vn/story/chapters?story_id={novelId}");
                    if (!response.IsSuccessStatusCode) return null;
                    var responseBody = await response.Content.ReadAsStringAsync();
                    document = new HtmlDocument();
                    document.LoadHtml(responseBody);

                    var chapterElements = document.DocumentNode.QuerySelectorAll("li.col-xs-6 a");
                    foreach (var element in chapterElements)
                    {
                        Chapter chapter = new Chapter();
                        // <a class="link-chap-539" href=" https://truyen.tangthuvien.vn/doc-truyen/dichtao-tac-suu-tam/chuong-560 " title="Chương 560&nbsp;:&nbsp;Trương Xương Bồ">
                        chapter.Title = HtmlEntity.DeEntitize(element.Attributes["title"].Value);
                        chapter.Slug = element.Attributes["href"].Value.Replace("https://truyen.tangthuvien.vn/doc-truyen/", "").Replace(novelSlug + "/", "").Trim();
                        chapter.Number = int.Parse(Regex.Match(chapter.Slug, @"\d+").Value);
                        listChapter.Add(chapter);
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
                var contentElement = document.DocumentNode.QuerySelector("div.box-chap");

                // Get content of chapter in html format
                if (contentElement != null)
                {
                    content = HtmlEntity.DeEntitize(contentElement.InnerHtml)
                        .Replace("\r\n", "<br/>").Replace("\\\"", "\"").Replace("\\t", "  ");
                }

                var titleElement = document.DocumentNode.QuerySelector("div.content div.col-xs-12.chapter h2");

                // Get title of chapter
                if (contentElement != null)
                {
                    title = HtmlEntity.DeEntitize(titleElement.InnerText)
                        .Replace("\r\n", "<br/>").Replace("\\\"", "\"").Replace("\\t", "  ");
                }

                // Get number of chapter: Ex: Chương 1: Trương Xương Bồ 2 -> 1
                var match = Regex.Match(title, @"\d+");
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

        public async Task<Chapter?> GetChapterAddrByNumber(string novelSlug, int? novelId, int chapterNumber)
        {
            Chapter[]? chapters = await CrawlListChapters(novelSlug, novelId != null ? novelId.ToString() : null);
            if (chapters == null) return null;


            var chapter = chapters.FirstOrDefault(x => x.Number == chapterNumber);
            if (chapter == null) return null;

            chapter.Source = "TruyenTangThuVienVn";
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

            if (listNovel.Count == 0 || (listNovel[0].Slug == null))
                return new Tuple<Novel[], int>(null, 0);

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
                var moreElement = document.DocumentNode.QuerySelector("p#update-tab a");
                if (moreElement != null)
                {
                    id = int.Parse(moreElement.Attributes["href"].Value.Replace("https://truyen.tangthuvien.vn/tong-hop?tp=cv&amp;ctg=", ""));
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
            [JsonPropertyName("id")]
            public int Id { get; set; }
            [JsonPropertyName("name")]
            public string Name { get; set; }
            [JsonPropertyName("url")]
            public string Url { get; set; }
            [JsonPropertyName("type")]
            public string Type { get; set; }
            [JsonPropertyName("story_type")]
            public int StoryType { get; set; }
        }

        #endregion
    }
}
