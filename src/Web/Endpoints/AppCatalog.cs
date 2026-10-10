using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.App;
using SurveillanceCameras.Application.Local.App.Commands;
using SurveillanceCameras.Application.Local.App.Queries;
using SurveillanceCameras.Infrastructure.Identity;

namespace SurveillanceCameras.Web.Endpoints;

/// <summary>
/// API cho app xem phim của người dùng cuối: danh mục phim đã xuất bản, các tập kèm quyền xem, bình luận. Đọc không cần
/// đăng nhập; có token thì quyền xem tính theo người đó.
/// </summary>
public class AppCatalog : IEndpointGroup
{
    public static string? RoutePrefix => "/api/app";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetAppConfig, "config");
        groupBuilder.MapGet(GetAppMovies, "movies");
        groupBuilder.MapGet(GetAppMovieById, "movies/{id}");
        groupBuilder.MapGet(GetAppMovieEpisodes, "movies/{id}/episodes");
        groupBuilder.MapGet(GetAppMovieComments, "movies/{id}/comments");
        groupBuilder.MapPost(AddAppMovieComment, "movies/{id}/comments").RequireAuthorization();
        groupBuilder.MapPost(UnlockAppEpisode, "episodes/{id}/unlock").RequireAuthorization();
    }

    public sealed record AddAppCommentRequest(string? Content);

    [EndpointSummary("Get app config")]
    [EndpointDescription("Cấu hình app lấy khi khởi động: số tập miễn phí / mở bằng quảng cáo, link pháp lý")]
    public static Ok<AppConfigDto> GetAppConfig(IOptions<AppOptions> options)
    {
        var o = options.Value;
        return TypedResults.Ok(new AppConfigDto(o.FreeEpisodes, o.AdEpisodes, o.PrivacyUrl, o.TermsUrl));
    }

    [EndpointSummary("Get app movies")]
    [EndpointDescription("Danh sách phim đã xuất bản (tìm theo tên/kênh, sắp xếp, có phân trang)")]
    public static async Task<Ok<PaginatedList<AppMovieDto>>> GetAppMovies(ISender sender, string? search = null,
        AppMovieSort sort = AppMovieSort.Newest, bool? full = null, int page = 1, int pageSize = 20)
        => TypedResults.Ok(await sender.Send(new GetAppMoviesQuery
        {
            Search = search, Sort = sort, Full = full, Page = page, PageSize = pageSize
        }));

    [EndpointSummary("Get app movie")]
    [EndpointDescription("Thông tin một phim đã xuất bản")]
    public static async Task<Ok<AppMovieDto>> GetAppMovieById(ISender sender, int id)
        => TypedResults.Ok(await sender.Send(new GetAppMovieByIdQuery(id)));

    [EndpointSummary("Get app movie episodes")]
    [EndpointDescription("Mọi tập của phim kèm quyền xem của người gọi; cách phát chỉ có ở tập xem được")]
    public static async Task<Ok<List<AppEpisodeDto>>> GetAppMovieEpisodes(ISender sender, int id)
        => TypedResults.Ok(await sender.Send(new GetAppMovieEpisodesQuery(id)));

    [EndpointSummary("Get app movie comments")]
    [EndpointDescription("Bình luận của phim, mới nhất trước")]
    public static async Task<Ok<PaginatedList<AppCommentDto>>> GetAppMovieComments(ISender sender, int id,
        int page = 1, int pageSize = 30)
        => TypedResults.Ok(await sender.Send(new GetAppCommentsQuery(id, page, pageSize)));

    [EndpointSummary("Add app movie comment")]
    [EndpointDescription("Người đang đăng nhập bình luận phim")]
    public static async Task<Ok<AppCommentDto>> AddAppMovieComment(ISender sender, IUser currentUser,
        UserManager<ApplicationUser> userManager, int id, AddAppCommentRequest request)
    {
        var user = await userManager.FindByIdAsync(currentUser.Id ?? "");
        return TypedResults.Ok(await sender.Send(new AddAppCommentCommand(id, request.Content, AppAuth.DisplayNameOf(user))));
    }

    [EndpointSummary("Unlock app episode")]
    [EndpointDescription("Mở một tập thuộc nhóm mở bằng quảng cáo cho người đang đăng nhập (gọi sau khi xem xong quảng cáo)")]
    public static async Task<Ok<AppEpisodeDto>> UnlockAppEpisode(ISender sender, int id)
        => TypedResults.Ok(await sender.Send(new UnlockAppEpisodeCommand(id)));
}
