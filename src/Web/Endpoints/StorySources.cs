using Microsoft.AspNetCore.Http.HttpResults;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.StorySources.Commands.CreateStorySource;
using SurveillanceCameras.Application.Local.StorySources.Commands.DeleteStorySource;
using SurveillanceCameras.Application.Local.StorySources.Commands.UpdateStorySource;
using SurveillanceCameras.Application.Local.StorySources.Queries.DTOs;
using SurveillanceCameras.Application.Local.StorySources.Queries.GetStorySourceById;
using SurveillanceCameras.Application.Local.StorySources.Queries.GetStorySources;

namespace SurveillanceCameras.Web.Endpoints;

public class StorySources : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetStorySources);
        groupBuilder.MapGet(GetStorySourceById, "{id}");
        groupBuilder.MapPost(CreateStorySource);
        groupBuilder.MapPut(UpdateStorySource, "{id}");
        groupBuilder.MapDelete(DeleteStorySource, "{id}");
    }

    [EndpointSummary("Get Story Sources")]
    [EndpointDescription("Lấy danh sách Story Source (lọc theo Web Source / tên, có phân trang)")]
    public static async Task<Ok<PaginatedList<StorySourceDto>>> GetStorySources(
        ISender sender, [AsParameters] GetStorySourcesQuery query)
    {
        var vm = await sender.Send(query);

        return TypedResults.Ok(vm);
    }

    [EndpointSummary("Get Story Source by Id")]
    [EndpointDescription("Lấy một Story Source theo ID")]
    public static async Task<Ok<StorySourceDto>> GetStorySourceById(ISender sender, int id)
    {
        var entity = await sender.Send(new GetStorySourceByIdQuery(id));

        return TypedResults.Ok(entity);
    }
    
    [EndpointSummary("Create Story Source")]
    [EndpointDescription("Tạo mới Story source")]
    public static async Task<Created<int>> CreateStorySource(ISender sender, CreateStorySourceCommand command)
    {
        var id = await sender.Send(command);
        
        return TypedResults.Created($"/{nameof(StorySources)}/{id}", id);
    }

    [EndpointSummary("Update Story Source")]
    [EndpointDescription("Updated Story Source")]
    public static async Task<Results<NoContent, BadRequest>> UpdateStorySource(ISender sender, int id, UpdateStorySourceCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();
        
        await sender.Send(command);
        
        return TypedResults.NoContent();
    }
    
    [EndpointSummary("Delete Story Source")]
    [EndpointDescription("Deleted Story Source")]
    public static async Task<NoContent> DeleteStorySource(ISender sender, int id)
    {
        await sender.Send(new DeleteStorySourceCommand(id));

        return TypedResults.NoContent();
    }
}
