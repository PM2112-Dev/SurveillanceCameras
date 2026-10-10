using Microsoft.AspNetCore.Http.HttpResults;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.UserProfileTypes.Commands.CreateUserProfileType;
using SurveillanceCameras.Application.Local.UserProfileTypes.Commands.DeleteUserProfileType;
using SurveillanceCameras.Application.Local.UserProfileTypes.Commands.UpdateUserProfileType;
using SurveillanceCameras.Application.Local.UserProfileTypes.Queries.DTOs;
using SurveillanceCameras.Application.Local.UserProfileTypes.Queries.GetUserProfileTypeById;
using SurveillanceCameras.Application.Local.UserProfileTypes.Queries.GetUserProfileTypes;

namespace SurveillanceCameras.Web.Endpoints;

public class UserProfileTypes : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetUserProfileTypes);
        groupBuilder.MapGet(GetUserProfileTypeById, "{id}");
        groupBuilder.MapPost(CreateUserProfileType);
        groupBuilder.MapPut(UpdateUserProfileType, "{id}");
        groupBuilder.MapDelete(DeleteUserProfileType, "{id}");
    }

    [EndpointSummary("Get UserProfileTypes")]
    [EndpointDescription("Lấy danh sách loại profile (lọc theo tên, có phân trang)")]
    public static async Task<Ok<PaginatedList<UserProfileTypeDto>>> GetUserProfileTypes(
        ISender sender, [AsParameters] GetUserProfileTypesQuery query)
    {
        var vm = await sender.Send(query);

        return TypedResults.Ok(vm);
    }

    [EndpointSummary("Get UserProfileType by Id")]
    [EndpointDescription("Lấy một loại profile theo ID")]
    public static async Task<Ok<UserProfileTypeDto>> GetUserProfileTypeById(ISender sender, int id)
    {
        var entity = await sender.Send(new GetUserProfileTypeByIdQuery(id));

        return TypedResults.Ok(entity);
    }

    [EndpointSummary("Create UserProfileType")]
    [EndpointDescription("Tạo mới loại profile")]
    public static async Task<Created<int>> CreateUserProfileType(ISender sender, CreateUserProfileTypeCommand command)
    {
        var id = await sender.Send(command);

        return TypedResults.Created($"/{nameof(UserProfileTypes)}/{id}", id);
    }

    [EndpointSummary("Update UserProfileType")]
    [EndpointDescription("Cập nhật loại profile")]
    public static async Task<Results<NoContent, BadRequest>> UpdateUserProfileType(
        ISender sender, int id, UpdateUserProfileTypeCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();

        await sender.Send(command);

        return TypedResults.NoContent();
    }

    [EndpointSummary("Delete UserProfileType")]
    [EndpointDescription("Xoá loại profile (xoá luôn các kênh đã lưu thuộc loại này)")]
    public static async Task<NoContent> DeleteUserProfileType(ISender sender, int id)
    {
        await sender.Send(new DeleteUserProfileTypeCommand(id));

        return TypedResults.NoContent();
    }
}
