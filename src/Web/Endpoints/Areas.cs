using Microsoft.AspNetCore.Http.HttpResults;
using SurveillanceCameras.Application.Areas.Commands.UpdateArea;
using SurveillanceCameras.Application.Areas.Queries;
using SurveillanceCameras.Application.Areas.Queries.GetAreaById;
using SurveillanceCameras.Application.Areas.Queries.GetAreas;
using SurveillanceCameras.Application.Areas.Queries.Model;
using SurveillanceCameras.Application.Local.Areas.Commands.CreateArea;
using SurveillanceCameras.Application.Local.Areas.Commands.DeleteArea;

namespace SurveillanceCameras.Web.Endpoints;

public class Areas : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetAreas);
        groupBuilder.MapGet(GetAreaById, "{id}");
        groupBuilder.MapPost(CreateArea);
        groupBuilder.MapPut(UpdateArea, "{id}");
        groupBuilder.MapDelete(DeleteArea, "{id}");
    }

    [EndpointSummary("Get all Areas")]
    [EndpointDescription("Retrieves all areas.")]
    public static async Task<Ok<AreasVm>> GetAreas(ISender sender)
    {
        var vm = await sender.Send(new GetAreasQuery());

        return TypedResults.Ok(vm);
    }

    [EndpointSummary("Get Area by Id")]
    [EndpointDescription("Retrieves one area by its ID.")]
    public static async Task<Ok<AreaDto>> GetAreaById(ISender sender, int id)
    {
        var area = await sender.Send(new GetAreaByIdQuery(id));

        return TypedResults.Ok(area);
    }

    [EndpointSummary("Create a new Area")]
    [EndpointDescription("Creates a new area using the provided details and returns the ID of the created area.")]
    public static async Task<Created<int>> CreateArea(ISender sender, CreateAreaCommand command)
    {
        var id = await sender.Send(command);
        
        return TypedResults.Created($"/{nameof(Areas)}/{id}", id);
    }

    [EndpointSummary("Update an Area")]
    [EndpointDescription("Updates the specified area. The ID in the URL must match the ID in the payload.")]
    public static async Task<Results<NoContent, BadRequest>> UpdateArea(ISender sender, int id,
        UpdateAreaCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();
        
        await sender.Send(command);
        
        return TypedResults.NoContent();
    }

    [EndpointSummary("Delete an Area")]
    [EndpointDescription("Deletes the area with the specified ID.")]
    public static async Task<NoContent> DeleteArea(ISender sender, int id)
    {
        await sender.Send(new DeleteAreaCommand(id));
        
        return TypedResults.NoContent();
    }
}
