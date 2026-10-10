using Microsoft.AspNetCore.Http.HttpResults;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.StoryVideoTypes.Commands.CreateStoryVideoType;
using SurveillanceCameras.Application.Local.StoryVideoTypes.Commands.DeleteStoryVideoType;
using SurveillanceCameras.Application.Local.StoryVideoTypes.Commands.UpdateStoryVideoType;
using SurveillanceCameras.Application.Local.StoryVideoTypes.Queries.DTOs;
using SurveillanceCameras.Application.Local.StoryVideoTypes.Queries.GetStoryVideoTypeById;
using SurveillanceCameras.Application.Local.StoryVideoTypes.Queries.GetStoryVideoTypes;

namespace SurveillanceCameras.Web.Endpoints;

public class StoryVideoTypes : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetStoryVideoTypes);
        groupBuilder.MapGet(GetStoryVideoTypeById, "{id}");
        groupBuilder.MapPost(CreateStoryVideoType);
        groupBuilder.MapPut(UpdateStoryVideoType, "{id}");
        groupBuilder.MapDelete(DeleteStoryVideoType, "{id}");
    }

    [EndpointSummary("Get StoryVideoTypes")]
    [EndpointDescription("Lấy danh sách loại video truyện (lọc theo tên, có phân trang)")]
    public static async Task<Ok<PaginatedList<StoryVideoTypeDto>>> GetStoryVideoTypes(
        ISender sender, [AsParameters] GetStoryVideoTypesQuery query)
    {
        var vm = await sender.Send(query);

        return TypedResults.Ok(vm);
    }

    [EndpointSummary("Get StoryVideoType by Id")]
    [EndpointDescription("Lấy một loại video truyện theo ID")]
    public static async Task<Ok<StoryVideoTypeDto>> GetStoryVideoTypeById(ISender sender, int id)
    {
        var entity = await sender.Send(new GetStoryVideoTypeByIdQuery(id));

        return TypedResults.Ok(entity);
    }

    [EndpointSummary("Create StoryVideoType")]
    [EndpointDescription("Tạo mới loại video truyện")]
    public static async Task<Created<int>> CreateStoryVideoType(ISender sender, CreateStoryVideoTypeCommand command)
    {
        var id = await sender.Send(command);

        return TypedResults.Created($"/{nameof(StoryVideoTypes)}/{id}", id);
    }

    [EndpointSummary("Update StoryVideoType")]
    [EndpointDescription("Cập nhật loại video truyện")]
    public static async Task<Results<NoContent, BadRequest>> UpdateStoryVideoType(
        ISender sender, int id, UpdateStoryVideoTypeCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();

        await sender.Send(command);

        return TypedResults.NoContent();
    }

    [EndpointSummary("Delete StoryVideoType")]
    [EndpointDescription("Xoá loại video truyện (truyện đang dùng sẽ bị gỡ liên kết)")]
    public static async Task<NoContent> DeleteStoryVideoType(ISender sender, int id)
    {
        await sender.Send(new DeleteStoryVideoTypeCommand(id));

        return TypedResults.NoContent();
    }
}
