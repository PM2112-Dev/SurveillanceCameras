using Microsoft.AspNetCore.Http.HttpResults;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.PromptTypes.Commands.CreatePromptType;
using SurveillanceCameras.Application.Local.PromptTypes.Commands.DeletePromptType;
using SurveillanceCameras.Application.Local.PromptTypes.Commands.UpdatePromptType;
using SurveillanceCameras.Application.Local.PromptTypes.Queries.DTOs;
using SurveillanceCameras.Application.Local.PromptTypes.Queries.GetPromptTypeById;
using SurveillanceCameras.Application.Local.PromptTypes.Queries.GetPromptTypes;

namespace SurveillanceCameras.Web.Endpoints;

public class PromptTypes : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetPromptTypes);
        groupBuilder.MapGet(GetPromptTypeById, "{id}");
        groupBuilder.MapPost(CreatePromptType);
        groupBuilder.MapPut(UpdatePromptType, "{id}");
        groupBuilder.MapDelete(DeletePromptType, "{id}");
    }

    [EndpointSummary("Get PromptTypes")]
    [EndpointDescription("Lấy danh sách PromptType (lọc theo tên, có phân trang)")]
    public static async Task<Ok<PaginatedList<PromptTypeDto>>> GetPromptTypes(
        ISender sender, [AsParameters] GetPromptTypesQuery query)
    {
        var vm = await sender.Send(query);

        return TypedResults.Ok(vm);
    }

    [EndpointSummary("Get PromptType by Id")]
    [EndpointDescription("Lấy một PromptType theo ID")]
    public static async Task<Ok<PromptTypeDto>> GetPromptTypeById(ISender sender, int id)
    {
        var entity = await sender.Send(new GetPromptTypeByIdQuery(id));

        return TypedResults.Ok(entity);
    }

    [EndpointSummary("Create PromptType")]
    [EndpointDescription("Tạo mới PromptType")]
    public static async Task<Created<int>> CreatePromptType(ISender sender, CreatePromptTypeCommand command)
    {
        var id = await sender.Send(command);

        return TypedResults.Created($"/{nameof(PromptTypes)}/{id}", id);
    }

    [EndpointSummary("Update PromptType")]
    [EndpointDescription("Cập nhật PromptType")]
    public static async Task<Results<NoContent, BadRequest>> UpdatePromptType(ISender sender, int id, UpdatePromptTypeCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();

        await sender.Send(command);

        return TypedResults.NoContent();
    }

    [EndpointSummary("Delete PromptType")]
    [EndpointDescription("Xoá PromptType")]
    public static async Task<NoContent> DeletePromptType(ISender sender, int id)
    {
        await sender.Send(new DeletePromptTypeCommand(id));

        return TypedResults.NoContent();
    }
}
