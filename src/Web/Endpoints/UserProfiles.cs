using Microsoft.AspNetCore.Http.HttpResults;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.Movies.Queries.GetChannelDramas;
using SurveillanceCameras.Application.Local.UserProfiles.Commands.DeleteUserProfile;
using SurveillanceCameras.Application.Local.UserProfiles.Queries.DTOs;
using SurveillanceCameras.Application.Local.UserProfiles.Queries.GetUserProfileById;
using SurveillanceCameras.Application.Local.UserProfiles.Queries.GetUserProfiles;

namespace SurveillanceCameras.Web.Endpoints;

/// <summary>Kênh mạng xã hội đã lưu. Dữ liệu được tạo/cập nhật khi tra cứu kênh, nên không có Create/Update.</summary>
public class UserProfiles : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetUserProfiles);
        groupBuilder.MapGet(GetUserProfileById, "{id}");
        groupBuilder.MapDelete(DeleteUserProfile, "{id}");
        groupBuilder.MapGet(GetChannelDramas, "{id}/dramas");
    }

    [EndpointSummary("Get channel dramas")]
    [EndpointDescription("Lấy một trang drama của kênh TikTok đã lưu (đọc thẳng từ TikTok), kèm phim đã đồng bộ")]
    public static async Task<Ok<ChannelDramasDto>> GetChannelDramas(ISender sender, int id, string cursor = "0", int count = 20)
        => TypedResults.Ok(await sender.Send(new GetChannelDramasQuery { UserProfileId = id, Cursor = cursor, Count = count }));

    [EndpointSummary("Get UserProfiles")]
    [EndpointDescription("Lấy danh sách kênh đã lưu (lọc theo tên/handle, loại, mã nền tảng; có phân trang)")]
    public static async Task<Ok<PaginatedList<UserProfileDto>>> GetUserProfiles(
        ISender sender, [AsParameters] GetUserProfilesQuery query)
    {
        var vm = await sender.Send(query);

        return TypedResults.Ok(vm);
    }

    [EndpointSummary("Get UserProfile by Id")]
    [EndpointDescription("Lấy một kênh đã lưu theo ID")]
    public static async Task<Ok<UserProfileDto>> GetUserProfileById(ISender sender, int id)
    {
        var entity = await sender.Send(new GetUserProfileByIdQuery(id));

        return TypedResults.Ok(entity);
    }

    [EndpointSummary("Delete UserProfile")]
    [EndpointDescription("Xoá kênh đã lưu")]
    public static async Task<NoContent> DeleteUserProfile(ISender sender, int id)
    {
        await sender.Send(new DeleteUserProfileCommand(id));

        return TypedResults.NoContent();
    }
}
