using NovelsCollector.Domain.Entities.Plugins.Exporters;
using NovelsCollector.Domain.Resources.Novels;
using System.Net;
using System.Text;

namespace Exporter.SimpleMp3
{
    public class SimpleMp3 : IExporterFeature
    {
        public async Task Export(Novel novel, Stream stream)
        {
            if (novel == null || novel.Chapters == null || novel.Chapters.Length == 0)
            {
                throw new ArgumentNullException(nameof(novel));
            }

            // Extract information
            var title = (novel.Title != null) ? novel.Title : "Không tựa đề";
            var author = (novel.Authors != null && novel.Authors.Length > 0) ? novel.Authors[0].Name : "Khuyết danh";
            var categories = (novel.Categories != null && novel.Categories.Length > 0) ?
                string.Join(", ", novel.Categories.Take(3).Select(c => c.Title)) :
                "Không thể loại";
            var startChapter = (novel.Chapters != null && novel.Chapters.Length > 0) ? novel.Chapters[0].Number : 1;
            var lastChapter = (novel.Chapters != null && novel.Chapters.Length > 0) ? novel.Chapters[^1].Number : 1;
            var chaptersRange = (startChapter == lastChapter) ? $"Chương {startChapter}" : $"Chương {startChapter} đến Chương {lastChapter}";
            var source = (novel.Source != null) ? novel.Source : "Không rõ";

            // Stringbuilder to build payload
            var payloadBuilder = new StringBuilder();
            payloadBuilder.AppendLine($"Tựa đề: {title}.");
            payloadBuilder.AppendLine($"Tác giả: {author}.");
            payloadBuilder.AppendLine($"Thể loại: {categories}.");
            payloadBuilder.AppendLine($"Đọc từ: {chaptersRange}.");
            payloadBuilder.AppendLine($"Nguồn: {source}.");
            payloadBuilder.AppendLine();

            foreach (var chapter in novel.Chapters)
            {
                payloadBuilder.AppendLine($"Chương {chapter.Number}: {chapter.Title}");
                payloadBuilder.AppendLine();
                // replace all ". ", ", ", ": ", " - ", "\n" into "." and ",", ":" and "-" and "\n"
                // replace all "..." into "."
                // replace all <,,,> tags into " "
                payloadBuilder.AppendLine(chapter.Content.Replace("\r\n\r\n", "\r\n").Replace("\n\n", "\n").Replace("  ", " ").Replace(". ", ".").Replace(", ", ",").Replace(": ", ":").Replace(" - ", "-").Replace("...", ".").Replace("<br>", "\r\n"));
            }

            // Final string payload
            var text = payloadBuilder.ToString();
            payloadBuilder.Clear();
            payloadBuilder = null;

            // Split text into parts
            var parts = buildParts(text);

            int c = 0;

            // Call API text-to-speech to convert each part into audio
            //foreach (var p in parts)

            if (parts.Length > 0)
            {
                var part = parts[0];
                c++;
                //if (c > 1) break; // for testing only

                using (var httpClient = new HttpClient(new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli }))
                {
                    // Add headers
                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");
                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "*/*");
                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate, br");
                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Host", "cloudtts.com");
                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Origin", "https://cloudtts.com");
                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://cloudtts.com/u/index.html");

                        
                    var payload = new
                    {
                        rate = 1,
                        volume = 1,
                        text = part,
                        voice = "vi-VN-HoaiMyNeural",
                        with_speechmarks = false,
                        recording = false
                    };
                    var payloadJSON = System.Text.Json.JsonSerializer.Serialize(payload);

                    try
                    {
                        // Call API
                        using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://cloudtts.com/api/get_audio"))
                        {
                            request.Content = new StringContent(payloadJSON, Encoding.UTF8, "application/json");

                            HttpResponseMessage response;
                            int count = 0;
                            do
                            {
                                count++;
                                response = await httpClient.SendAsync(request);
                            } while (!response.IsSuccessStatusCode && count < 10);

                            var responseContent = await response.Content.ReadAsStringAsync();
                            var responseJSON = System.Text.Json.JsonSerializer.Deserialize<ResponseRoot>(responseContent);

                            if (responseJSON.success)
                            {
                                var audioBytes = Convert.FromBase64String(responseJSON.data.audio);

                                // Append this part to stream
                                await stream.WriteAsync(audioBytes, 0, audioBytes.Length);
                                await stream.FlushAsync();

                            }
                            else
                            {
                                throw new Exception($"Failed to convert text to audio unsuccessfully. Response: {System.Text.Json.JsonSerializer.Serialize(response)}");
                            }

                            // Free all of resources
                            response.Dispose();
                            request.Dispose();
                            responseContent = null;
                            responseJSON = null;

                            GC.Collect();
                            GC.WaitForPendingFinalizers();


                        }
                    }
                    catch (Exception ex)
                    {
                        httpClient.Dispose();
                        throw;
                    }

                    // Free all of resources
                    httpClient.Dispose();
                    payload = null;
                    payloadJSON = null;
                    text = null;
                    parts = null;
                    part = null;

                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                }
            }



        }

        private string[] buildParts(string text)
        {
            var parts = new List<string>();
            const int MAX_LENGTH = 3500;
            if (text.Length < MAX_LENGTH)
            {
                parts.Add(text);
            }
            else
            {
                var part = new StringBuilder();
                var partLength = 0;

                foreach (var line in text.Split("\r\n"))
                {
                    if (partLength + Encoding.UTF8.GetByteCount(line) > MAX_LENGTH)
                    {
                        parts.Add(part.ToString());
                        part.Clear();
                        partLength = 0;
                    }

                    part.AppendLine(line);
                    partLength += Encoding.UTF8.GetByteCount(line);
                }

                parts.Add(part.ToString());
                part.Clear();
                part = null;
            }

            return parts.ToArray();
        }

        private class Speechmarks
        {
            public int offset { get; set; }
            public string word { get; set; }
        }

        private class Data
        {
            public string audio { get; set; }
            public List<Speechmarks> speechmarks { get; set; }
        }

        private class ResponseRoot
        {
            public Data data { get; set; }
            public string msg { get; set; }
            public bool success { get; set; }
        }
    }
}
