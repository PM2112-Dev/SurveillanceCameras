using Microsoft.AspNetCore.Http.HttpResults;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.Chapters.Commands.CreateChapter;
using SurveillanceCameras.Application.Local.Chapters.Commands.DeleteChapter;
using SurveillanceCameras.Application.Local.Chapters.Commands.UpdateChapter;
using SurveillanceCameras.Application.Local.Chapters.Queries.DTOs;
using SurveillanceCameras.Application.Local.Chapters.Queries.GetChapterById;
using SurveillanceCameras.Application.Local.Chapters.Queries.GetChapters;

namespace SurveillanceCameras.Web.Endpoints;

public class Chapters : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetChapters);
        groupBuilder.MapGet(GetChapterById, "{id}");
        groupBuilder.MapPost(CreateChapter);
        groupBuilder.MapPut(UpdateChapter, "{id}");
        groupBuilder.MapDelete(DeleteChapter, "{id}");
    }

    [EndpointSummary("Get Chapters")]
    [EndpointDescription("Lấy danh sách Chapter (lọc theo tên, có phân trang)")]
    public static async Task<Ok<PaginatedList<ChapterDto>>> GetChapters(
        ISender sender, [AsParameters] GetChaptersQuery query)
    {
        var vm = await sender.Send(query);

        return TypedResults.Ok(vm);
    }

    [EndpointSummary("Get Chapter by Id")]
    [EndpointDescription("Lấy một Chapter theo ID")]
    public static async Task<Ok<ChapterDto>> GetChapterById(ISender sender, int id)
    {
        var entity = await sender.Send(new GetChapterByIdQuery(id));

        return TypedResults.Ok(entity);
    }

    [EndpointSummary("Create Chapter")]
    [EndpointDescription("Tạo mới Chapter")]
    public static async Task<Created<int>> CreateChapter(ISender sender, CreateChapterCommand command)
    {
        var id = await sender.Send(command);

        return TypedResults.Created($"/{nameof(Chapters)}/{id}", id);
    }

    [EndpointSummary("Update Chapter")]
    [EndpointDescription("Cập nhật Chapter")]
    public static async Task<Results<NoContent, BadRequest>> UpdateChapter(ISender sender, int id, UpdateChapterCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();

        await sender.Send(command);

        return TypedResults.NoContent();
    }

    [EndpointSummary("Delete Chapter")]
    [EndpointDescription("Xoá Chapter")]
    public static async Task<NoContent> DeleteChapter(ISender sender, int id)
    {
        await sender.Send(new DeleteChapterCommand(id));

        return TypedResults.NoContent();
    }
}
