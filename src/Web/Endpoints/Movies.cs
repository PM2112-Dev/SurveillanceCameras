using Microsoft.AspNetCore.Http.HttpResults;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.Movies.Commands.CleanupMovieEpisodes;
using SurveillanceCameras.Application.Local.Movies.Commands.DeleteMovie;
using SurveillanceCameras.Application.Local.Movies.Commands.DeleteMovieEpisodes;
using SurveillanceCameras.Application.Local.Movies.Commands.UpdateMovieEpisode;
using SurveillanceCameras.Application.Local.Movies.Commands.RetryMovieVideoStorage;
using SurveillanceCameras.Application.Local.Movies.Commands.SetMovieAccess;
using SurveillanceCameras.Application.Local.Movies.Commands.SetMovieEpisodeStream;
using SurveillanceCameras.Application.Local.Movies.Commands.SetMoviePublished;
using SurveillanceCameras.Application.Local.Movies.Commands.StoreMovieEpisodeVideo;
using SurveillanceCameras.Application.Local.Movies.Commands.SyncTikTokDramas;
using SurveillanceCameras.Application.Local.Movies.Queries.DTOs;
using SurveillanceCameras.Application.Local.Movies.Queries.GetMovieById;
using SurveillanceCameras.Application.Local.Movies.Queries.GetMovieEpisodes;
using SurveillanceCameras.Application.Local.Movies.Queries.GetMovies;

namespace SurveillanceCameras.Web.Endpoints;

/// <summary>Phim (tác phẩm loại Phim) đồng bộ từ drama của kênh TikTok đã lưu.</summary>
public class Movies : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetMovies);
        groupBuilder.MapGet(GetMovieById, "{id}");
        groupBuilder.MapGet(GetMovieEpisodes, "{id}/episodes");
        groupBuilder.MapPost(SyncTikTokDramas, "sync-tiktok");
        groupBuilder.MapDelete(DeleteMovie, "{id}");
        groupBuilder.MapPost(SetMoviesPublished, "published");
        groupBuilder.MapPut(SetMovieEpisodeStream, "{id}/episodes/{episodeId}/stream");
        groupBuilder.MapPut(SetMovieAccess, "{id}/access");
        groupBuilder.MapPost(RetryMovieVideoStorage, "{id}/video-storage/retry");
        groupBuilder.MapPut(UpdateMovieEpisode, "{id}/episodes/{episodeId}");
        groupBuilder.MapPost(DeleteMovieEpisodes, "{id}/episodes/delete");
        groupBuilder.MapPost(CleanupMovieEpisodes, "{id}/episodes/cleanup");
        groupBuilder.MapPost(StoreMovieEpisodeVideo, "{id}/episodes/{episodeId}/video-storage/store");
    }

    public sealed record SetMoviesPublishedRequest(bool IsPublished, List<int>? Ids);

    public sealed record SetMovieEpisodeStreamRequest(string? StreamUrl);

    public sealed record UpdateMovieEpisodeRequest(int EpisodeNumber, string? Name);

    public sealed record DeleteMovieEpisodesRequest(List<int> EpisodeIds);

    public sealed record CleanupMovieEpisodesRequest(bool Apply, bool RemoveBroken = true, bool Renumber = false);

    [EndpointSummary("Update Movie episode")]
    [EndpointDescription("Sửa số tập và tên của một tập phim; tập đã sửa không bị đồng bộ lại từ TikTok ghi đè")]
    public static async Task<NoContent> UpdateMovieEpisode(ISender sender, int id, int episodeId, UpdateMovieEpisodeRequest request)
    {
        await sender.Send(new UpdateMovieEpisodeCommand(id, episodeId, request.EpisodeNumber, request.Name));
        return TypedResults.NoContent();
    }

    [EndpointSummary("Delete Movie episodes")]
    [EndpointDescription("Xoá các tập phim (kèm file video đã lưu); đồng bộ lại không thêm các tập này trở lại. Trả số tập đã xoá")]
    public static async Task<Ok<int>> DeleteMovieEpisodes(ISender sender, int id, DeleteMovieEpisodesRequest request)
        => TypedResults.Ok(await sender.Send(new DeleteMovieEpisodesCommand(id, request.EpisodeIds)));

    [EndpointSummary("Cleanup Movie episodes")]
    [EndpointDescription("Dọn tập lỗi/trùng của phim: xoá bản sao cùng số tập, tập không còn nguồn phát, cho tập lưu video lỗi thử lại, đánh số lại tuỳ chọn. apply=false chỉ chạy thử")]
    public static async Task<Ok<CleanupMovieEpisodesResult>> CleanupMovieEpisodes(ISender sender, int id, CleanupMovieEpisodesRequest request)
        => TypedResults.Ok(await sender.Send(new CleanupMovieEpisodesCommand
            { MovieId = id, Apply = request.Apply, RemoveBroken = request.RemoveBroken, Renumber = request.Renumber }));

    [EndpointSummary("Set Movies published")]
    [EndpointDescription("Bật/tắt hiển thị phim trên app xem phim; không truyền ids thì áp dụng cho mọi phim. Trả về số phim đã đổi")]
    public static async Task<Ok<int>> SetMoviesPublished(ISender sender, SetMoviesPublishedRequest request)
        => TypedResults.Ok(await sender.Send(new SetMoviePublishedCommand(request.IsPublished, request.Ids)));

    public sealed record SetMovieAccessRequest(bool UseDefault, int? AdFromEpisode, int? AdToEpisode, int? VipFromEpisode,
        int? VipToEpisode, bool IsVipOnly = false);

    [EndpointSummary("Set Movie access")]
    [EndpointDescription("Đặt quyền xem của phim trên app: khoảng tập quảng cáo/VIP (useDefault = true để dùng mặc định hệ thống; cấu hình riêng không có khoảng nào là miễn phí mọi tập) và isVipOnly để cả phim chỉ dành cho VIP")]
    public static async Task<NoContent> SetMovieAccess(ISender sender, int id, SetMovieAccessRequest request)
    {
        await sender.Send(new SetMovieAccessCommand
        {
            MovieId = id, UseDefault = request.UseDefault, IsVipOnly = request.IsVipOnly,
            AdFromEpisode = request.AdFromEpisode, AdToEpisode = request.AdToEpisode,
            VipFromEpisode = request.VipFromEpisode, VipToEpisode = request.VipToEpisode
        });

        return TypedResults.NoContent();
    }

    [EndpointSummary("Set Movie episode stream")]
    [EndpointDescription("Gán link file video tự lưu trữ (https, mp4/HLS) cho một tập; bỏ trống để gỡ")]
    public static async Task<NoContent> SetMovieEpisodeStream(ISender sender, int id, int episodeId,
        SetMovieEpisodeStreamRequest request)
    {
        await sender.Send(new SetMovieEpisodeStreamCommand(id, episodeId, request.StreamUrl));

        return TypedResults.NoContent();
    }

    [EndpointSummary("Retry Movie video storage")]
    [EndpointDescription("Cho hệ thống thử tải lại video của các tập đã lưu lỗi trong phim. Trả về số tập được đưa lại vào hàng chờ")]
    public static async Task<Ok<int>> RetryMovieVideoStorage(ISender sender, int id)
        => TypedResults.Ok(await sender.Send(new RetryMovieVideoStorageCommand(id)));

    [EndpointSummary("Store Movie episode video")]
    [EndpointDescription("Tải video của một tập từ TikTok (bằng cookie tài khoản TikTok) và lưu vào kho ngay; trả về link phát hoặc lý do lỗi")]
    public static async Task<Ok<StoreMovieEpisodeVideoResult>> StoreMovieEpisodeVideo(ISender sender, int id, int episodeId)
        => TypedResults.Ok(await sender.Send(new StoreMovieEpisodeVideoCommand(id, episodeId)));

    [EndpointSummary("Get Movies")]
    [EndpointDescription("Lấy danh sách phim (lọc theo tên, kênh; có phân trang)")]
    public static async Task<Ok<PaginatedList<MovieDto>>> GetMovies(ISender sender, [AsParameters] GetMoviesQuery query)
        => TypedResults.Ok(await sender.Send(query));

    [EndpointSummary("Get Movie by Id")]
    [EndpointDescription("Lấy thông tin một phim")]
    public static async Task<Ok<MovieDto>> GetMovieById(ISender sender, int id)
        => TypedResults.Ok(await sender.Send(new GetMovieByIdQuery(id)));

    [EndpointSummary("Get Movie episodes")]
    [EndpointDescription("Lấy các tập của phim theo thứ tự tập")]
    public static async Task<Ok<PaginatedList<MovieEpisodeDto>>> GetMovieEpisodes(ISender sender, int id,
        int page = 1, int pageSize = 50, bool onlyIssues = false)
        => TypedResults.Ok(await sender.Send(new GetMovieEpisodesQuery { MovieId = id, Page = page, PageSize = pageSize, OnlyIssues = onlyIssues }));

    [EndpointSummary("Sync TikTok dramas")]
    [EndpointDescription("Đồng bộ các drama đã chọn của kênh TikTok thành phim cùng các tập (đã có thì cập nhật, thêm tập mới)")]
    public static async Task<Ok<SyncTikTokDramasResult>> SyncTikTokDramas(ISender sender, SyncTikTokDramasCommand command)
        => TypedResults.Ok(await sender.Send(command));

    [EndpointSummary("Delete Movie")]
    [EndpointDescription("Xoá phim và các tập trong hệ thống")]
    public static async Task<NoContent> DeleteMovie(ISender sender, int id)
    {
        await sender.Send(new DeleteMovieCommand(id));

        return TypedResults.NoContent();
    }
}
