using Microsoft.AspNetCore.Http.HttpResults;
using SurveillanceCameras.Application.Local.Stories.Commands.UpdateStory;
using SurveillanceCameras.Application.Local.Stories.Queries.Model;
using SurveillanceCameras.Application.Stories.Commands.CreateStory;
using SurveillanceCameras.Application.Stories.Commands.DeleteStory;
using SurveillanceCameras.Application.Stories.Queries.GetStories;
using SurveillanceCameras.Application.Stories.Queries.GetStoryById;

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

    [EndpointSummary("Get all stories")]
    [EndpointDescription("Lấy tất cả truyện theo user hiện tại")]
    public static async Task<Ok<StoryVm>> GetStories(ISender sender)
    {
        var vm = await sender.Send(new GetStoriesQuery());
        
        return TypedResults.Ok(vm);
    }

    [EndpointSummary("Get Story by Id")]
    [EndpointDescription("Lấy một truyện theo ID")]
    public static async Task<Ok<StoryDto>> GetStoryById(ISender sender, int id)
    {
        var story = await sender.Send(new GetStoryByIdQuery(id));

        return TypedResults.Ok(story);
    }

    [EndpointSummary("Create a new story")]
    [EndpointDescription("Tạo mới truyện với các thông tin được cung cấp và trả về ID của truyện đã tạo.")]
    public static async Task<Created<int>> CreateStory(ISender sender, CreateStoryCommand command)
    {
        var id = await sender.Send(command);
        
        return TypedResults.Created($"/{nameof(Stories)}/{id}", id);
    }

    [EndpointSummary("Update a story")]
    [EndpointDescription(
        "Cập nhật truyện đã tồn tại với các thông tin được cung cấp và trả về ID của truyện đã cập nhật.")]
    public static async Task<Results<NoContent, BadRequest>> UpdateStory(ISender sender, int id,
        UpdateStoryCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();
        
        await sender.Send(command);
        
        return TypedResults.NoContent();
    }

    [EndpointSummary("Delete a story")]
    [EndpointDescription("Xóa truyện đã tồn tại với ID được cung cấp.")]
    public static async Task<Results<NoContent, BadRequest>> DeleteStory(ISender sender, int id)
    {
        await sender.Send(new DeleteStoryCommand(id));

        return TypedResults.NoContent();
    }
}
