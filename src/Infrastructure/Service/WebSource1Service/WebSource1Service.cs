using System.Net;
using HtmlAgilityPack;
using SurveillanceCameras.Application.CrawlData;
using SurveillanceCameras.Application.Service.WikiDichService;
using StoryDataDto = SurveillanceCameras.Application.CrawlData.StoryDataDto;

namespace SurveillanceCameras.Infrastructure.Service.WebSource1Service;

public class WebSource1Service : ICrawlDataService
{
    private readonly HttpClient _httpClient;
    
    public WebSource1Service(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<StoryListPageDto> FetchStory(string url, int webSourceId, CancellationToken cancellationToken = default)
    {
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

        var html = await _httpClient.GetStringAsync(url, cancellationToken);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var result = new StoryListPageDto();

        // Story cards: <a class="cover-wrapper ..." href="..." title="Tên truyện">...</a>
        var storyNodes = doc.DocumentNode.SelectNodes("//a[contains(@class,'cover-wrapper')]");
        if (storyNodes != null)
        {
            foreach (var node in storyNodes)
            {
                var link = node.GetAttributeValue("href", null!);
                if (string.IsNullOrWhiteSpace(link)) continue;

                var title = WebUtility.HtmlDecode(node.GetAttributeValue("title", null!));
                if (string.IsNullOrWhiteSpace(title))
                {
                    // Fallback if the anchor itself has no title: use the cover image's alt text.
                    title = WebUtility.HtmlDecode(node.SelectSingleNode(".//img")?.GetAttributeValue("alt", null!));
                }

                result.Stories.Add(new StoryListItemDto
                {
                    Title = title,
                    Link = link
                });
            }
        }

        // Pagination: <a href=".../trang-{n}">{n}</a>. There's no dedicated "next" button in the
        // markup we have, so find the highest page number linked from this page's own pagination
        // nav and, if that's beyond the current page, build the next page's URL by pattern.
        var currentPageMatch = System.Text.RegularExpressions.Regex.Match(url, @"/trang-(\d+)");
        var currentPage = currentPageMatch.Success ? int.Parse(currentPageMatch.Groups[1].Value) : 1;

        var pageLinkNodes = doc.DocumentNode.SelectNodes("//a[contains(@href,'/trang-')]");
        var maxPage = currentPage;
        if (pageLinkNodes != null)
        {
            foreach (var node in pageLinkNodes)
            {
                var href = node.GetAttributeValue("href", null!);
                var match = System.Text.RegularExpressions.Regex.Match(href ?? string.Empty, @"/trang-(\d+)");
                if (match.Success && int.TryParse(match.Groups[1].Value, out var pageNumber) && pageNumber > maxPage)
                {
                    maxPage = pageNumber;
                }
            }
        }

        if (maxPage > currentPage)
        {
            result.NextPageUrl = System.Text.RegularExpressions.Regex.Replace(url, @"/trang-\d+", $"/trang-{currentPage + 1}");
        }

        return result;
    }

    public Task<StoryDataDto> FetchStoryData(string linkUrl, int webSourceId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<ChapterDataDto> FetchChapterData(string linkUrl, int webSourceId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
