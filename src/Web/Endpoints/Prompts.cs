using Microsoft.AspNetCore.Http.HttpResults;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.Prompts.Commands.CreatePrompt;
using SurveillanceCameras.Application.Local.Prompts.Commands.DeletePrompt;
using SurveillanceCameras.Application.Local.Prompts.Commands.UpdatePrompt;
using SurveillanceCameras.Application.Local.Prompts.Queries.DTOs;
using SurveillanceCameras.Application.Local.Prompts.Queries.GetPromptById;
using SurveillanceCameras.Application.Local.Prompts.Queries.GetPrompts;

namespace SurveillanceCameras.Web.Endpoints;

public class Prompts : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetPrompts);
        groupBuilder.MapGet(GetPromptById, "{id}");
        groupBuilder.MapPost(CreatePrompt);
        groupBuilder.MapPut(UpdatePrompt, "{id}");
        groupBuilder.MapDelete(DeletePrompt, "{id}");
    }

    [EndpointSummary("Get Prompts")]
    [EndpointDescription("Lấy danh sách Prompt (lọc theo tên, có phân trang)")]
    public static async Task<Ok<PaginatedList<PromptDto>>> GetPrompts(
        ISender sender, [AsParameters] GetPromptsQuery query)
    {
        var vm = await sender.Send(query);

        return TypedResults.Ok(vm);
    }

    [EndpointSummary("Get Prompt by Id")]
    [EndpointDescription("Lấy một Prompt theo ID")]
    public static async Task<Ok<PromptDto>> GetPromptById(ISender sender, int id)
    {
        var entity = await sender.Send(new GetPromptByIdQuery(id));

        return TypedResults.Ok(entity);
    }

    [EndpointSummary("Create Prompt")]
    [EndpointDescription("Tạo mới Prompt")]
    public static async Task<Created<int>> CreatePrompt(ISender sender, CreatePromptCommand command)
    {
        var id = await sender.Send(command);

        return TypedResults.Created($"/{nameof(Prompts)}/{id}", id);
    }

    [EndpointSummary("Update Prompt")]
    [EndpointDescription("Cập nhật Prompt")]
    public static async Task<Results<NoContent, BadRequest>> UpdatePrompt(ISender sender, int id, UpdatePromptCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();

        await sender.Send(command);

        return TypedResults.NoContent();
    }

    [EndpointSummary("Delete Prompt")]
    [EndpointDescription("Xoá Prompt")]
    public static async Task<NoContent> DeletePrompt(ISender sender, int id)
    {
        await sender.Send(new DeletePromptCommand(id));

        return TypedResults.NoContent();
    }
}
