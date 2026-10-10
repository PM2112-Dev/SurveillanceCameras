using Microsoft.AspNetCore.Http.HttpResults;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.Movies.Commands.DeleteMovie;
using SurveillanceCameras.Application.Local.Movies.Commands.SetMovieEpisodeStream;
using SurveillanceCameras.Application.Local.Movies.Commands.SetMoviePublished;
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
    }

    public sealed record SetMoviesPublishedRequest(bool IsPublished, List<int>? Ids);

    public sealed record SetMovieEpisodeStreamRequest(string? StreamUrl);

    [EndpointSummary("Set Movies published")]
    [EndpointDescription("Bật/tắt hiển thị phim trên app xem phim; không truyền ids thì áp dụng cho mọi phim. Trả về số phim đã đổi")]
    public static async Task<Ok<int>> SetMoviesPublished(ISender sender, SetMoviesPublishedRequest request)
        => TypedResults.Ok(await sender.Send(new SetMoviePublishedCommand(request.IsPublished, request.Ids)));

    [EndpointSummary("Set Movie episode stream")]
    [EndpointDescription("Gán link file video tự lưu trữ (https, mp4/HLS) cho một tập; bỏ trống để gỡ")]
    public static async Task<NoContent> SetMovieEpisodeStream(ISender sender, int id, int episodeId,
        SetMovieEpisodeStreamRequest request)
    {
        await sender.Send(new SetMovieEpisodeStreamCommand(id, episodeId, request.StreamUrl));

        return TypedResults.NoContent();
    }

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
        int page = 1, int pageSize = 50)
        => TypedResults.Ok(await sender.Send(new GetMovieEpisodesQuery { MovieId = id, Page = page, PageSize = pageSize }));

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
