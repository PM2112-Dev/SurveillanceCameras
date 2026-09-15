using Microsoft.AspNetCore.Http.HttpResults;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.AccountTypes.Commands.CreateAccountType;
using SurveillanceCameras.Application.Local.AccountTypes.Commands.DeleteAccountType;
using SurveillanceCameras.Application.Local.AccountTypes.Commands.UpdateAccountType;
using SurveillanceCameras.Application.Local.AccountTypes.Queries.DTOs;
using SurveillanceCameras.Application.Local.AccountTypes.Queries.GetAccountTypeById;
using SurveillanceCameras.Application.Local.AccountTypes.Queries.GetAccountTypes;

namespace SurveillanceCameras.Web.Endpoints;

public class AccountTypes : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetAccountTypes);
        groupBuilder.MapGet(GetAccountTypeById, "{id}");
        groupBuilder.MapPost(CreateAccountType);
        groupBuilder.MapPut(UpdateAccountType, "{id}");
        groupBuilder.MapDelete(DeleteAccountType, "{id}");
    }

    [EndpointSummary("Get AccountTypes")]
    [EndpointDescription("Lấy danh sách AccountType (lọc theo tên, có phân trang)")]
    public static async Task<Ok<PaginatedList<AccountTypeDto>>> GetAccountTypes(
        ISender sender, [AsParameters] GetAccountTypesQuery query)
    {
        var vm = await sender.Send(query);

        return TypedResults.Ok(vm);
    }

    [EndpointSummary("Get AccountType by Id")]
    [EndpointDescription("Lấy một AccountType theo ID")]
    public static async Task<Ok<AccountTypeDto>> GetAccountTypeById(ISender sender, int id)
    {
        var entity = await sender.Send(new GetAccountTypeByIdQuery(id));

        return TypedResults.Ok(entity);
    }

    [EndpointSummary("Create AccountType")]
    [EndpointDescription("Tạo mới AccountType")]
    public static async Task<Created<int>> CreateAccountType(ISender sender, CreateAccountTypeCommand command)
    {
        var id = await sender.Send(command);

        return TypedResults.Created($"/{nameof(AccountTypes)}/{id}", id);
    }

    [EndpointSummary("Update AccountType")]
    [EndpointDescription("Cập nhật AccountType")]
    public static async Task<Results<NoContent, BadRequest>> UpdateAccountType(ISender sender, int id, UpdateAccountTypeCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();

        await sender.Send(command);

        return TypedResults.NoContent();
    }

    [EndpointSummary("Delete AccountType")]
    [EndpointDescription("Xoá AccountType")]
    public static async Task<NoContent> DeleteAccountType(ISender sender, int id)
    {
        await sender.Send(new DeleteAccountTypeCommand(id));

        return TypedResults.NoContent();
    }
}
