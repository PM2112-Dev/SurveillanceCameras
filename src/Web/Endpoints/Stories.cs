using Microsoft.AspNetCore.Http.HttpResults;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.Stories.Commands.CreateStory;
using SurveillanceCameras.Application.Local.Stories.Commands.DeleteStory;
using SurveillanceCameras.Application.Local.Stories.Commands.UpdateStory;
using SurveillanceCameras.Application.Local.Stories.Queries.DTOs;
using SurveillanceCameras.Application.Local.Stories.Queries.GetStories;
using SurveillanceCameras.Application.Local.Stories.Queries.GetStoryById;

namespace SurveillanceCameras.Web.Endpoints;

public class Stories : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetStories);
        groupBuilder.MapGet(GetStoryById, "{id}");
        groupBuilder.MapPost(CreateStory);
        groupBuilder.MapPut(UpdateStory, "{id}");
        groupBuilder.MapDelete(DeleteStory, "{id}");
    }

    [EndpointSummary("Get Stories")]
    [EndpointDescription("Lấy danh sách Story (lọc theo tên, có phân trang)")]
    public static async Task<Ok<PaginatedList<StoryDto>>> GetStories(
        ISender sender, [AsParameters] GetStoriesQuery query)
    {
        var vm = await sender.Send(query);

        return TypedResults.Ok(vm);
    }

    [EndpointSummary("Get Story by Id")]
    [EndpointDescription("Lấy một Story theo ID")]
    public static async Task<Ok<StoryDto>> GetStoryById(ISender sender, int id)
    {
        var entity = await sender.Send(new GetStoryByIdQuery(id));

        return TypedResults.Ok(entity);
    }

    [EndpointSummary("Create Story")]
    [EndpointDescription("Tạo mới Story")]
    public static async Task<Created<int>> CreateStory(ISender sender, CreateStoryCommand command)
    {
        var id = await sender.Send(command);

        return TypedResults.Created($"/{nameof(Stories)}/{id}", id);
    }

    [EndpointSummary("Update Story")]
    [EndpointDescription("Cập nhật Story")]
    public static async Task<Results<NoContent, BadRequest>> UpdateStory(ISender sender, int id, UpdateStoryCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();

        await sender.Send(command);

        return TypedResults.NoContent();
    }

    [EndpointSummary("Delete Story")]
    [EndpointDescription("Xoá Story")]
    public static async Task<NoContent> DeleteStory(ISender sender, int id)
    {
        await sender.Send(new DeleteStoryCommand(id));

        return TypedResults.NoContent();
    }
}
