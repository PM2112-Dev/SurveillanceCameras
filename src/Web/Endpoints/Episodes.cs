using Microsoft.AspNetCore.Http.HttpResults;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.Episodes.Commands.CreateEpisode;
using SurveillanceCameras.Application.Local.Episodes.Commands.DeleteEpisode;
using SurveillanceCameras.Application.Local.Episodes.Commands.RequestEpisodeEdit;
using SurveillanceCameras.Application.Local.Episodes.Commands.RequestTytChapterUpdate;
using SurveillanceCameras.Application.Local.Episodes.Commands.UpdateEpisode;
using SurveillanceCameras.Application.Local.Episodes.Queries.DTOs;
using SurveillanceCameras.Application.Local.Episodes.Queries.GetEpisodeById;
using SurveillanceCameras.Application.Local.Episodes.Queries.GetEpisodes;

namespace SurveillanceCameras.Web.Endpoints;

public class Episodes : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetEpisodes);
        groupBuilder.MapGet(GetEpisodeById, "{id}");
        groupBuilder.MapPost(CreateEpisode);
        groupBuilder.MapPut(UpdateEpisode, "{id}");
        groupBuilder.MapDelete(DeleteEpisode, "{id}");
        groupBuilder.MapPost(RequestEpisodeEdit, "{id}/ai-edit");
        groupBuilder.MapPost(RequestTytChapterUpdate, "{id}/tyt-update");
    }

    [EndpointSummary("Request episode AI edit")]
    [EndpointDescription("AI biên tập lại nội dung chương (chạy nền qua Kafka)")]
    public static async Task<Accepted> RequestEpisodeEdit(ISender sender, int id)
    {
        await sender.Send(new RequestEpisodeEditCommand(id));

        return TypedResults.Accepted((string?)null);
    }

    [EndpointSummary("Get Episodes")]
    [EndpointDescription("Lấy danh sách chương/tập của một tác phẩm (có phân trang)")]
    public static async Task<Ok<PaginatedList<EpisodeBriefDto>>> GetEpisodes(
        ISender sender, [AsParameters] GetEpisodesQuery query)
    {
        var vm = await sender.Send(query);

        return TypedResults.Ok(vm);
    }

    [EndpointSummary("Get Episode by Id")]
    [EndpointDescription("Lấy một chương/tập theo ID")]
    public static async Task<Ok<EpisodeDto>> GetEpisodeById(ISender sender, int id)
    {
        var entity = await sender.Send(new GetEpisodeByIdQuery(id));

        return TypedResults.Ok(entity);
    }

    [EndpointSummary("Create Episode")]
    [EndpointDescription("Tạo mới chương")]
    public static async Task<Created<int>> CreateEpisode(ISender sender, CreateEpisodeCommand command)
    {
        var id = await sender.Send(command);

        return TypedResults.Created($"/{nameof(Episodes)}/{id}", id);
    }

    [EndpointSummary("Update Episode")]
    [EndpointDescription("Cập nhật chương")]
    public static async Task<Results<NoContent, BadRequest>> UpdateEpisode(ISender sender, int id, UpdateEpisodeCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();

        await sender.Send(command);

        return TypedResults.NoContent();
    }

    [EndpointSummary("Delete Episode")]
    [EndpointDescription("Xoá chương/tập")]
    public static async Task<NoContent> DeleteEpisode(ISender sender, int id)
    {
        await sender.Send(new DeleteEpisodeCommand(id));

        return TypedResults.NoContent();
    }

    [EndpointSummary("Request TYT chapter update")]
    [EndpointDescription("Cập nhật lại chương đã đăng lên TYT bằng nội dung đã biên tập hiện tại (chạy nền)")]
    public static async Task<Accepted> RequestTytChapterUpdate(ISender sender, int id)
    {
        await sender.Send(new RequestTytChapterUpdateCommand(id));

        return TypedResults.Accepted((string?)null);
    }
}
