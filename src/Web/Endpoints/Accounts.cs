using Microsoft.AspNetCore.Http.HttpResults;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.Accounts.Commands.CreateAccount;
using SurveillanceCameras.Application.Local.Accounts.Commands.DeleteAccount;
using SurveillanceCameras.Application.Local.Accounts.Commands.StartAccountSession;
using SurveillanceCameras.Application.Local.Accounts.Commands.StopAccountSession;
using SurveillanceCameras.Application.Local.Accounts.Commands.UpdateAccount;
using SurveillanceCameras.Application.Local.Accounts.Queries.DTOs;
using SurveillanceCameras.Application.Local.Accounts.Queries.GetAccountById;
using SurveillanceCameras.Application.Local.Accounts.Queries.GetAccounts;
using SurveillanceCameras.Application.Local.Accounts.Queries.GetAccountSessionStatus;
using SurveillanceCameras.Application.Common.Models.TikTok;
using SurveillanceCameras.Application.Local.TikTok.Queries.GetTiktokDramaList;
using SurveillanceCameras.Application.Local.TikTok.Commands.SearchTiktokUserProfile;
using SurveillanceCameras.Application.Repository.AccountSession;

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
        groupBuilder.MapPost(StartAccountSession, "{id}/session/start");
        groupBuilder.MapPost(StopAccountSession, "{id}/session/stop");
        groupBuilder.MapGet(GetAccountSessionStatus, "{id}/session");
        groupBuilder.MapGet(GetTikTokDramaList, "{id}/tiktok/dramas");
        groupBuilder.MapPost(SearchTikTokUserProfile, "{id}/tiktok/users/{handle}/search");
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

    [EndpointSummary("Start Account Session")]
    [EndpointDescription("Mở Chrome trên server tại trang đăng nhập của loại tài khoản (AccountType.BaseUrl) và tự cập nhật cookie vào Account khi cookie thay đổi")]
    public static async Task<NoContent> StartAccountSession(ISender sender, int id)
    {
        await sender.Send(new StartAccountSessionCommand(id));

        return TypedResults.NoContent();
    }

    [EndpointSummary("Stop Account Session")]
    [EndpointDescription("Đóng Chrome của account (profile được giữ lại nên lần sau vẫn còn đăng nhập)")]
    public static async Task<NoContent> StopAccountSession(ISender sender, int id)
    {
        await sender.Send(new StopAccountSessionCommand(id));

        return TypedResults.NoContent();
    }

    [EndpointSummary("Get Account Session Status")]
    [EndpointDescription("Trạng thái phiên đăng nhập: Stopped / WaitingForLogin / Active / LoggedOut và thời điểm cập nhật cookie gần nhất")]
    public static async Task<Ok<AccountSessionStatus>> GetAccountSessionStatus(ISender sender, int id)
    {
        var status = await sender.Send(new GetAccountSessionStatusQuery(id));

        return TypedResults.Ok(status);
    }

    [EndpointSummary("Get TikTok Drama List")]
    [EndpointDescription("Lấy danh sách drama của một kênh TikTok (theo secUid) bằng cookie của Account")]
    public static async Task<Ok<DramaListDto>> GetTikTokDramaList(
        ISender sender, int id, string secUid, int count = 20, string cursor = "0")
    {
        var result = await sender.Send(new GetTiktokDramaListQuery
        {
            AccountId = id, SecUid = secUid, Count = count, Cursor = cursor
        });

        return TypedResults.Ok(result);
    }

    [EndpointSummary("Search TikTok User Profile")]
    [EndpointDescription("Tra cứu kênh TikTok (secUid, follower, số video…) từ handle bằng Chrome headless và lưu vào UserProfile")]
    public static async Task<Ok<TikTokUserProfileDto>> SearchTikTokUserProfile(ISender sender, int id, string handle)
    {
        var result = await sender.Send(new SearchTiktokUserProfileCommand { AccountId = id, Handle = handle });

        return TypedResults.Ok(result);
    }
}
