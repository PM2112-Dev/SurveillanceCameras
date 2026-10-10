using Microsoft.AspNetCore.Http.HttpResults;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.CrawlStories.Commands.SyncCrawledStories;
using SurveillanceCameras.Application.Local.CrawlStories.Queries.GetCrawledChapter;
using SurveillanceCameras.Application.Local.CrawlStories.Queries.GetCrawledChapters;
using SurveillanceCameras.Application.Local.CrawlStories.Queries.GetCrawledStories;
using SurveillanceCameras.Application.Local.CrawlStories.Queries.GetCrawledStory;
using SurveillanceCameras.Application.Repository.CrawlStory.DTOs;
using SurveillanceCameras.Application.Local.WebSources.Commands.CreateWebSource;
using SurveillanceCameras.Application.Local.WebSources.Commands.DeleteWebSource;
using SurveillanceCameras.Application.Local.WebSources.Commands.UpdateWebSource;
using SurveillanceCameras.Application.Local.WebSources.Queries.DTOs;
using SurveillanceCameras.Application.Local.WebSources.Queries.GetWebSourceById;
using SurveillanceCameras.Application.Local.WebSources.Queries.GetWebSources;

namespace SurveillanceCameras.Web.Endpoints;

public class WebSources : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetWebSource);
        groupBuilder.MapGet(GetWebSourceById, "{id}");
        groupBuilder.MapPost(CreateWebSource);
        groupBuilder.MapPost(UpdateWebSource, "{id}");
        groupBuilder.MapDelete(DeleteWebSource, "{id}");

        // Đọc thẳng từ trang nguồn theo cấu hình crawl, không lưu DB.
        groupBuilder.MapGet(GetCrawledStories, "{id}/crawl/stories");
        groupBuilder.MapGet(GetCrawledStory, "{id}/crawl/story");
        groupBuilder.MapGet(GetCrawledChapters, "{id}/crawl/chapters");
        groupBuilder.MapGet(GetCrawledChapter, "{id}/crawl/chapter");
        groupBuilder.MapPost(SyncCrawledStories, "{id}/crawl/sync");
    }

    [EndpointSummary("Sync crawled stories")]
    [EndpointDescription("Lưu các truyện đã chọn từ danh sách crawl vào StorySource (đã có thì cập nhật, không tạo trùng)")]
    public static async Task<Results<Ok<SyncCrawledStoriesResult>, BadRequest>> SyncCrawledStories(
        ISender sender, int id, SyncCrawledStoriesCommand command)
    {
        if (id != command.WebSourceId) return TypedResults.BadRequest();

        var result = await sender.Send(command);

        return TypedResults.Ok(result);
    }

    [EndpointSummary("Get crawled stories")]
    [EndpointDescription("Lấy một trang danh sách truyện trực tiếp từ trang nguồn của Web Source (không lưu DB)")]
    public static async Task<Ok<CrawlPageDto<StoryCrawlDto>>> GetCrawledStories(ISender sender, int id, int page = 1)
    {
        var result = await sender.Send(new GetCrawledStoriesQuery { WebSourceId = id, Page = page });

        return TypedResults.Ok(result);
    }

    [EndpointSummary("Get crawled story")]
    [EndpointDescription("Lấy thông tin truyện trực tiếp từ trang nguồn (url phải thuộc host của Web Source)")]
    public static async Task<Ok<StoryDataDto>> GetCrawledStory(ISender sender, int id, string url)
    {
        var result = await sender.Send(new GetCrawledStoryQuery { WebSourceId = id, Url = url });

        return TypedResults.Ok(result);
    }

    [EndpointSummary("Get crawled chapters")]
    [EndpointDescription("Lấy một trang mục lục chương của truyện trực tiếp từ trang nguồn")]
    public static async Task<Ok<CrawlPageDto<ChapterDataDto>>> GetCrawledChapters(
        ISender sender, int id, string url, int page = 1)
    {
        var result = await sender.Send(new GetCrawledChaptersQuery { WebSourceId = id, Url = url, Page = page });

        return TypedResults.Ok(result);
    }

    [EndpointSummary("Get crawled chapter")]
    [EndpointDescription("Lấy nội dung một chương trực tiếp từ trang nguồn")]
    public static async Task<Ok<ChapterContentDto>> GetCrawledChapter(ISender sender, int id, string url)
    {
        var result = await sender.Send(new GetCrawledChapterQuery { WebSourceId = id, Url = url });

        return TypedResults.Ok(result);
    }
    
    [EndpointSummary("Get WebSources")]
    [EndpointDescription("Get Web Sources")]
    public static async Task<Ok<PaginatedList<WebSourceDto>>> GetWebSource(
        ISender sender, [AsParameters] GetWebSourcesQuery query)
    {
        var vm = await sender.Send(query);

        return TypedResults.Ok(vm);
    }
    
    [EndpointSummary("Get Web Source by Id")]
    [EndpointDescription("Lấy một Web Source theo ID")]
    public static async Task<Ok<WebSourceDto>> GetWebSourceById(ISender sender, int id)
    {
        var entity = await sender.Send(new GetWebSourceIdQuery(id));

        return TypedResults.Ok(entity);
    }
    
    [EndpointSummary("Create Web Source")]
    [EndpointDescription("Tạo mới web source")]
    public static async Task<Created<int>> CreateWebSource(ISender sender, CreateWebSourceCommand command)
    {
        var id = await sender.Send(command);
        
        return TypedResults.Created($"/{nameof(WebSources)}/{id}", id);
    }
    
    [EndpointSummary("Update Web Source")]
    [EndpointDescription("Cập nhật web source")]
    public static async Task<Results<NoContent, BadRequest>> UpdateWebSource(ISender sender, int id, UpdateWebSourceCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();
        
        await sender.Send(command);
        
        return TypedResults.NoContent();
    }
    
    [EndpointSummary("Delete Web Source")]
    [EndpointDescription("Xoá web source")]
    public static async Task<NoContent> DeleteWebSource(ISender sender, int id)
    {
        await sender.Send(new DeleteWebSourceCommand(id));

        return TypedResults.NoContent();
    }
}
