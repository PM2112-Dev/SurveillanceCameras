using Microsoft.AspNetCore.Http.HttpResults;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.AIs.Commands.CreateAI;
using SurveillanceCameras.Application.Local.AIs.Commands.DeleteAI;
using SurveillanceCameras.Application.Local.AIs.Commands.UpdateAI;
using SurveillanceCameras.Application.Local.AIs.Queries.DTOs;
using SurveillanceCameras.Application.Local.AIs.Queries.GetAIById;
using SurveillanceCameras.Application.Local.AIs.Queries.GetAIs;

namespace SurveillanceCameras.Web.Endpoints;

public class AIs : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetAIs);
        groupBuilder.MapGet(GetAIById, "{id}");
        groupBuilder.MapPost(CreateAI);
        groupBuilder.MapPut(UpdateAI, "{id}");
        groupBuilder.MapDelete(DeleteAI, "{id}");
    }

    [EndpointSummary("Get AIs")]
    [EndpointDescription("Lấy danh sách AI (lọc theo tên, có phân trang)")]
    public static async Task<Ok<PaginatedList<AIDto>>> GetAIs(
        ISender sender, [AsParameters] GetAIsQuery query)
    {
        var vm = await sender.Send(query);

        return TypedResults.Ok(vm);
    }

    [EndpointSummary("Get AI by Id")]
    [EndpointDescription("Lấy một AI theo ID")]
    public static async Task<Ok<AIDto>> GetAIById(ISender sender, int id)
    {
        var entity = await sender.Send(new GetAIByIdQuery(id));

        return TypedResults.Ok(entity);
    }

    [EndpointSummary("Create AI")]
    [EndpointDescription("Tạo mới AI")]
    public static async Task<Created<int>> CreateAI(ISender sender, CreateAICommand command)
    {
        var id = await sender.Send(command);

        return TypedResults.Created($"/{nameof(AIs)}/{id}", id);
    }

    [EndpointSummary("Update AI")]
    [EndpointDescription("Cập nhật AI")]
    public static async Task<Results<NoContent, BadRequest>> UpdateAI(ISender sender, int id, UpdateAICommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();

        await sender.Send(command);

        return TypedResults.NoContent();
    }

    [EndpointSummary("Delete AI")]
    [EndpointDescription("Xoá AI")]
    public static async Task<NoContent> DeleteAI(ISender sender, int id)
    {
        await sender.Send(new DeleteAICommand(id));

        return TypedResults.NoContent();
    }
}
