using Microsoft.AspNetCore.Http.HttpResults;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.Accounts.Commands.CreateAccount;
using SurveillanceCameras.Application.Local.Accounts.Commands.DeleteAccount;
using SurveillanceCameras.Application.Local.Accounts.Commands.StartTikTokSession;
using SurveillanceCameras.Application.Local.Accounts.Commands.StopTikTokSession;
using SurveillanceCameras.Application.Local.Accounts.Commands.UpdateAccount;
using SurveillanceCameras.Application.Local.Accounts.Queries.DTOs;
using SurveillanceCameras.Application.Local.Accounts.Queries.GetAccountById;
using SurveillanceCameras.Application.Local.Accounts.Queries.GetAccounts;
using SurveillanceCameras.Application.Local.Accounts.Queries.GetTikTokDramaList;
using SurveillanceCameras.Application.Local.Accounts.Queries.GetTikTokUserProfile;
using SurveillanceCameras.Application.Local.Accounts.Queries.GetTikTokSessionStatus;
using SurveillanceCameras.Application.Service.TikTokApi;
using SurveillanceCameras.Application.Service.TikTokSession;
using SurveillanceCameras.Application.TikTokApi;

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
        groupBuilder.MapPost(StartTikTokSession, "{id}/tiktok-session/start");
        groupBuilder.MapPost(StopTikTokSession, "{id}/tiktok-session/stop");
        groupBuilder.MapGet(GetTikTokSessionStatus, "{id}/tiktok-session");
        groupBuilder.MapGet(GetTikTokDramaList, "{id}/tiktok/dramas");
        groupBuilder.MapGet(GetTikTokUserProfile, "{id}/tiktok/users/{handle}");
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

    [EndpointSummary("Start TikTok Session")]
    [EndpointDescription("Mở Chrome trên server, giữ kết nối TikTok và tự cập nhật cookie vào Account khi cookie thay đổi")]
    public static async Task<NoContent> StartTikTokSession(ISender sender, int id)
    {
        await sender.Send(new StartTikTokSessionCommand(id));

        return TypedResults.NoContent();
    }

    [EndpointSummary("Stop TikTok Session")]
    [EndpointDescription("Đóng Chrome của account (profile được giữ lại nên lần sau vẫn còn đăng nhập)")]
    public static async Task<NoContent> StopTikTokSession(ISender sender, int id)
    {
        await sender.Send(new StopTikTokSessionCommand(id));

        return TypedResults.NoContent();
    }

    [EndpointSummary("Get TikTok Session Status")]
    [EndpointDescription("Trạng thái phiên TikTok: Stopped / WaitingForLogin / Active / LoggedOut và thời điểm cập nhật cookie gần nhất")]
    public static async Task<Ok<TikTokSessionStatus>> GetTikTokSessionStatus(ISender sender, int id)
    {
        var status = await sender.Send(new GetTikTokSessionStatusQuery(id));

        return TypedResults.Ok(status);
    }

    [EndpointSummary("Get TikTok Drama List")]
    [EndpointDescription("Lấy danh sách drama của một kênh TikTok (theo secUid) bằng cookie của Account")]
    public static async Task<Ok<DramaListDto>> GetTikTokDramaList(
        ISender sender, int id, string secUid, int count = 20, string cursor = "0")
    {
        var result = await sender.Send(new GetTikTokDramaListQuery(id, secUid, count, cursor));

        return TypedResults.Ok(result);
    }

    [EndpointSummary("Get TikTok User Profile")]
    [EndpointDescription("Lấy thông tin kênh TikTok (secUid, follower, số video…) từ handle bằng Chrome headless")]
    public static async Task<Ok<TikTokUserProfileDto>> GetTikTokUserProfile(ISender sender, int id, string handle)
    {
        var result = await sender.Send(new GetTikTokUserProfileQuery(id, handle));

        return TypedResults.Ok(result);
    }
}
