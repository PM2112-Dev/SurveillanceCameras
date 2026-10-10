using Microsoft.AspNetCore.Http.HttpResults;
using SurveillanceCameras.Application.Local.Episodes.Commands.RequestWorkEpisodeSync;
using SurveillanceCameras.Application.Local.Episodes.Commands.RetryWorkEpisodes;
using SurveillanceCameras.Application.Local.Works.Commands.RequestTytStoryCreation;
using SurveillanceCameras.Application.Local.Episodes.Queries.GetWorkEpisodeStats;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.Works.Commands.CreateWork;
using SurveillanceCameras.Application.Local.Works.Commands.DeleteWork;
using SurveillanceCameras.Application.Local.Works.Commands.UpdateWork;
using SurveillanceCameras.Application.Local.Works.Queries.DTOs;
using SurveillanceCameras.Application.Local.Works.Queries.GetWorks;
using SurveillanceCameras.Application.Local.Works.Queries.GetWorkById;

namespace SurveillanceCameras.Web.Endpoints;

public class Works : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetWorks);
        groupBuilder.MapGet(GetWorkById, "{id}");
        groupBuilder.MapPost(CreateWork);
        groupBuilder.MapPut(UpdateWork, "{id}");
        groupBuilder.MapDelete(DeleteWork, "{id}");
        groupBuilder.MapGet(GetWorkEpisodeStats, "{id}/episode-stats");
        groupBuilder.MapPost(RequestWorkEpisodeSync, "{id}/sync-episodes");
        groupBuilder.MapPost(RetryWorkEpisodes, "{id}/retry-episodes");
        groupBuilder.MapPost(RequestTytStoryCreation, "{id}/tyt");
    }

    [EndpointSummary("Get work episode stats")]
    [EndpointDescription("Tiến độ lấy/biên tập chương của truyện: đếm theo trạng thái và lỗi đồng bộ mục lục")]
    public static async Task<Ok<WorkEpisodeStatsDto>> GetWorkEpisodeStats(ISender sender, int id)
    {
        var stats = await sender.Send(new GetWorkEpisodeStatsQuery(id));

        return TypedResults.Ok(stats);
    }

    [EndpointSummary("Request work episode sync")]
    [EndpointDescription("Đồng bộ lại mục lục chương từ trang nguồn (lấy chương mới) — chạy nền qua Kafka")]
    public static async Task<Accepted> RequestWorkEpisodeSync(ISender sender, int id)
    {
        await sender.Send(new RequestWorkEpisodeSyncCommand(id));

        return TypedResults.Accepted((string?)null);
    }

    [EndpointSummary("Request TYT story creation")]
    [EndpointDescription("Đưa truyện lên TYT bằng tài khoản của truyện (chạy nền): chưa có thì tạo và lưu Id TYT, sau đó cập nhật thông tin truyện")]
    public static async Task<Accepted> RequestTytStoryCreation(ISender sender, int id)
    {
        await sender.Send(new RequestTytStoryCreationCommand(id));

        return TypedResults.Accepted((string?)null);
    }

    [EndpointSummary("Retry work episodes")]
    [EndpointDescription("Tải lại chương lỗi/chưa tải và biên tập lại chương lỗi/chưa biên tập — chạy nền qua Kafka. Trả số chương được đưa vào hàng đợi")]
    public static async Task<Ok<int>> RetryWorkEpisodes(ISender sender, int id)
    {
        var count = await sender.Send(new RetryWorkEpisodesCommand(id));

        return TypedResults.Ok(count);
    }

    [EndpointSummary("Get Works")]
    [EndpointDescription("Lấy danh sách tác phẩm loại Truyện (lọc theo tên, có phân trang)")]
    public static async Task<Ok<PaginatedList<WorkDto>>> GetWorks(
        ISender sender, [AsParameters] GetWorksQuery query)
    {
        var vm = await sender.Send(query);

        return TypedResults.Ok(vm);
    }

    [EndpointSummary("Get Work by Id")]
    [EndpointDescription("Lấy một tác phẩm theo ID")]
    public static async Task<Ok<WorkDto>> GetWorkById(ISender sender, int id)
    {
        var entity = await sender.Send(new GetWorkByIdQuery(id));

        return TypedResults.Ok(entity);
    }

    [EndpointSummary("Create Work")]
    [EndpointDescription("Tạo mới truyện")]
    public static async Task<Created<int>> CreateWork(ISender sender, CreateWorkCommand command)
    {
        var id = await sender.Send(command);

        return TypedResults.Created($"/{nameof(Works)}/{id}", id);
    }

    [EndpointSummary("Update Work")]
    [EndpointDescription("Cập nhật tác phẩm")]
    public static async Task<Results<NoContent, BadRequest>> UpdateWork(ISender sender, int id, UpdateWorkCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();

        await sender.Send(command);

        return TypedResults.NoContent();
    }

    [EndpointSummary("Delete Work")]
    [EndpointDescription("Xoá tác phẩm")]
    public static async Task<NoContent> DeleteWork(ISender sender, int id)
    {
        await sender.Send(new DeleteWorkCommand(id));

        return TypedResults.NoContent();
    }
}
