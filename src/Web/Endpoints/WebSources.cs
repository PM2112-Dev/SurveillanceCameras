using Microsoft.AspNetCore.Http.HttpResults;
using SurveillanceCameras.Application.Common.Models;
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
