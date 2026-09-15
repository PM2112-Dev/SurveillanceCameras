using Microsoft.AspNetCore.Http.HttpResults;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.Accounts.Commands.CreateAccount;
using SurveillanceCameras.Application.Local.Accounts.Commands.DeleteAccount;
using SurveillanceCameras.Application.Local.Accounts.Commands.UpdateAccount;
using SurveillanceCameras.Application.Local.Accounts.Queries.DTOs;
using SurveillanceCameras.Application.Local.Accounts.Queries.GetAccountById;
using SurveillanceCameras.Application.Local.Accounts.Queries.GetAccounts;

namespace SurveillanceCameras.Web.Endpoints;

public class Accounts : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetAccounts);
        groupBuilder.MapGet(GetAccountById, "{id}");
        groupBuilder.MapPost(CreateAccount);
        groupBuilder.MapPut(UpdateAccount, "{id}");
        groupBuilder.MapDelete(DeleteAccount, "{id}");
    }

    [EndpointSummary("Get Accounts")]
    [EndpointDescription("Lấy danh sách Account (lọc theo tên, có phân trang)")]
    public static async Task<Ok<PaginatedList<AccountDto>>> GetAccounts(
        ISender sender, [AsParameters] GetAccountsQuery query)
    {
        var vm = await sender.Send(query);

        return TypedResults.Ok(vm);
    }

    [EndpointSummary("Get Account by Id")]
    [EndpointDescription("Lấy một Account theo ID")]
    public static async Task<Ok<AccountDto>> GetAccountById(ISender sender, int id)
    {
        var entity = await sender.Send(new GetAccountByIdQuery(id));

        return TypedResults.Ok(entity);
    }

    [EndpointSummary("Create Account")]
    [EndpointDescription("Tạo mới Account")]
    public static async Task<Created<int>> CreateAccount(ISender sender, CreateAccountCommand command)
    {
        var id = await sender.Send(command);

        return TypedResults.Created($"/{nameof(Accounts)}/{id}", id);
    }

    [EndpointSummary("Update Account")]
    [EndpointDescription("Cập nhật Account")]
    public static async Task<Results<NoContent, BadRequest>> UpdateAccount(ISender sender, int id, UpdateAccountCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();

        await sender.Send(command);

        return TypedResults.NoContent();
    }

    [EndpointSummary("Delete Account")]
    [EndpointDescription("Xoá Account")]
    public static async Task<NoContent> DeleteAccount(ISender sender, int id)
    {
        await sender.Send(new DeleteAccountCommand(id));

        return TypedResults.NoContent();
    }
}
