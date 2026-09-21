// using System.Net.Http;
// using System.Net;
// using System.Threading;
// using System.Threading.Tasks;
// using HtmlAgilityPack;
// using SurveillanceCameras.Application.CrawlData;
// using SurveillanceCameras.Application.WikiDichService;
// using StoryDataDto = SurveillanceCameras.Application.Service.WikiDichService.StoryDataDto;
//
// namespace SurveillanceCameras.Infrastructure.Service.WikiDichService;
//
// public class WikiDichApiService : IWikiDichApiService
// {
//     private readonly HttpClient _httpClient;
//     
//     private static readonly string[] ListGenres = new[]
//     {
//         "Ngôn Tình", "Hệ Thống", "Xuyên Không", "Xuyên Nhanh", "Đam Mỹ", "Bách Hợp", "Truyện Teen", "Tiên Hiệp",
//         "Kiếm Hiệp", "Huyền Huyễn", "Khác", "Mạt Thế", "Trinh Thám", "Đô Thị", "Võng Du", "Khoa Huyễn", "Hiện Đại",
//         "Ngược", "Điền Văn", "Sảng Văn", "Hài Hước", "Cung Đấu", "Cổ Đại", "Trọng Sinh", "Quan Trường", "Nữ Cường",
//         "Nữ Phụ", "Sủng", "Gia Đấu", "Võng Du"
//     };
//     
//     public WikiDichApiService(HttpClient httpClient)
//     {
//         _httpClient = httpClient;
//     }
//
//     public async Task<StoryDataDto> FetchStory(string url, CancellationToken cancellationToken = default)
//     {
//         _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
//
//         var html = await _httpClient.GetStringAsync(url, cancellationToken);
//         var doc = new HtmlDocument();
//         doc.LoadHtml(html);
//
//         var story = new StoryDataDto();
//
//         // Title
//         story.Title = WebUtility.HtmlDecode(doc.DocumentNode.SelectSingleNode("//div[contains(@class,'mRightCol')]//h1")?.InnerText.Trim() ?? string.Empty);
//
//         // Author
//         story.Author = WebUtility.HtmlDecode(doc.DocumentNode.SelectSingleNode("//li/a[@itemprop='author']")?.InnerText.Trim() ?? string.Empty);
//
//         // Total Chapters
//         var chapterNodes = doc.DocumentNode.SelectNodes("//div[contains(@class,'book-info-text')]//li");
//         if (chapterNodes != null)
//         {
//             foreach (var node in chapterNodes)
//             {
//                 var text = node.InnerText.Trim();
//                 if (text.Contains("Số chương:"))
//                 {
//                     // Ưu tiên tách chuỗi sau 'Số chương:'
//                     var idx = text.IndexOf(":");
//                     if (idx >= 0 && idx + 1 < text.Length)
//                     {
//                         var afterColon = text.Substring(idx + 1).Trim();
//                         // Lấy số đầu tiên sau dấu :
//                         var match = System.Text.RegularExpressions.Regex.Match(afterColon, @"\d+");
//                         if (match.Success)
//                         {
//                             story.TotalChapters = int.Parse(match.Value);
//                         }
//                         else
//                         {
//                             // Log giá trị thực tế nếu không parse được
//                             System.Diagnostics.Debug.WriteLine($"[WikiDichApiService] Không parse được TotalChapters từ: '{afterColon}'");
//                         }
//                     }
//                     else
//                     {
//                         // Fallback: dùng regex toàn bộ text
//                         var match = System.Text.RegularExpressions.Regex.Match(text, @"\d+");
//                         if (match.Success)
//                         {
//                             story.TotalChapters = int.Parse(match.Value);
//                         }
//                         else
//                         {
//                             System.Diagnostics.Debug.WriteLine($"[WikiDichApiService] Không parse được TotalChapters từ: '{text}'");
//                         }
//                     }
//                     break;
//                 }
//             }
//         }
//
//         // Description
//         story.Description = WebUtility.HtmlDecode(
//             doc.DocumentNode.SelectSingleNode("//div[@id='gioithieu']//*[@itemprop='description']")
//             ?.InnerText.Trim().Replace("\n", " ").Replace("\r", " ") ?? string.Empty
//         );
//
//         // Image
//         var imgNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'book-info-pic')]//img");
//         var imgUrl = imgNode?.GetAttributeValue("src", null!);
//         if (imgUrl != null && imgUrl.StartsWith("/"))
//         {
//             var baseUrl = new Uri(url).GetLeftPart(UriPartial.Authority);
//             imgUrl = baseUrl + imgUrl;
//         }
//         story.ImageUrl = imgUrl;
//
//         // Link chương 1
//         var linkNode = doc.DocumentNode.SelectSingleNode("//a[span[contains(@class,'btn_truyen') and normalize-space(text())='Chương 1']]");
//         story.Link = linkNode?.GetAttributeValue("href", null!);
//
//         // Genres
//         var genreNodes = doc.DocumentNode.SelectNodes("//div[contains(@class,'book-info-text')]//li[contains(@class,'li--genres')]//a");
//         if (genreNodes != null)
//         {
//             foreach (var node in genreNodes)
//             {
//                 var genre = WebUtility.HtmlDecode(node.InnerText.Trim());
//                 if (ListGenres.Contains(genre) && !story.Genres.Contains(genre))
//                     story.Genres.Add(genre);
//             }
//         }
//
//         return story;
//     }
//
//     public async Task<StoryListPageDto> FetchStoryList(string url, CancellationToken cancellationToken = default)
//     {
//         _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
//
//         var html = await _httpClient.GetStringAsync(url, cancellationToken);
//         var doc = new HtmlDocument();
//         doc.LoadHtml(html);
//
//         var result = new StoryListPageDto();
//
//         // Story cards: <a class="cover-wrapper ..." href="..." title="Tên truyện">...</a>
//         var storyNodes = doc.DocumentNode.SelectNodes("//a[contains(@class,'cover-wrapper')]");
//         if (storyNodes != null)
//         {
//             foreach (var node in storyNodes)
//             {
//                 var link = node.GetAttributeValue("href", null!);
//                 if (string.IsNullOrWhiteSpace(link)) continue;
//
//                 var title = WebUtility.HtmlDecode(node.GetAttributeValue("title", null!));
//                 if (string.IsNullOrWhiteSpace(title))
//                 {
//                     // Fallback if the anchor itself has no title: use the cover image's alt text.
//                     title = WebUtility.HtmlDecode(node.SelectSingleNode(".//img")?.GetAttributeValue("alt", null!));
//                 }
//
//                 result.Stories.Add(new StoryListItemDto
//                 {
//                     Title = title,
//                     Link = link
//                 });
//             }
//         }
//
//         // Pagination: <a href=".../trang-{n}">{n}</a>. There's no dedicated "next" button in the
//         // markup we have, so find the highest page number linked from this page's own pagination
//         // nav and, if that's beyond the current page, build the next page's URL by pattern.
//         var currentPageMatch = System.Text.RegularExpressions.Regex.Match(url, @"/trang-(\d+)");
//         var currentPage = currentPageMatch.Success ? int.Parse(currentPageMatch.Groups[1].Value) : 1;
//
//         var pageLinkNodes = doc.DocumentNode.SelectNodes("//a[contains(@href,'/trang-')]");
//         var maxPage = currentPage;
//         if (pageLinkNodes != null)
//         {
//             foreach (var node in pageLinkNodes)
//             {
//                 var href = node.GetAttributeValue("href", null!);
//                 var match = System.Text.RegularExpressions.Regex.Match(href ?? string.Empty, @"/trang-(\d+)");
//                 if (match.Success && int.TryParse(match.Groups[1].Value, out var pageNumber) && pageNumber > maxPage)
//                 {
//                     maxPage = pageNumber;
//                 }
//             }
//         }
//
//         if (maxPage > currentPage)
//         {
//             result.NextPageUrl = System.Text.RegularExpressions.Regex.Replace(url, @"/trang-\d+", $"/trang-{currentPage + 1}");
//         }
//
//         return result;
//     }
// }
