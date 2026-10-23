using Microsoft.AspNetCore.Http.HttpResults;
using SurveillanceCameras.Application.Local.StorageAccounts.Commands.CheckStorageAccount;
using SurveillanceCameras.Application.Local.StorageAccounts.Commands.CreateStorageAccount;
using SurveillanceCameras.Application.Local.StorageAccounts.Commands.DeleteStorageAccount;
using SurveillanceCameras.Application.Local.StorageAccounts.Commands.TrimStorageAccount;
using SurveillanceCameras.Application.Local.StorageAccounts.Commands.UpdateStorageAccount;
using SurveillanceCameras.Application.Local.StorageAccounts.Commands.UpdateStorageAccountMonthlyStat;
using SurveillanceCameras.Application.Local.StorageAccounts.Queries.DTOs;
using SurveillanceCameras.Application.Local.StorageAccounts.Queries.GetStorageAccountById;
using SurveillanceCameras.Application.Local.StorageAccounts.Queries.GetStorageAccounts;

namespace SurveillanceCameras.Web.Endpoints;

/// <summary>Tài khoản Cloudflare R2 duy nhất dùng để lưu file video của tập phim và theo dõi chi phí/request hằng tháng.</summary>
public class StorageAccounts : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetStorageAccounts);
        groupBuilder.MapGet(GetStorageAccountById, "{id}");
        groupBuilder.MapPost(CreateStorageAccount);
        groupBuilder.MapPut(UpdateStorageAccount, "{id}");
        groupBuilder.MapDelete(DeleteStorageAccount, "{id}");
        groupBuilder.MapPost(CheckStorageAccount, "{id}/check");
        groupBuilder.MapPost(TrimStorageAccount, "{id}/trim");
        groupBuilder.MapPut(UpdateStorageAccountMonthlyStat, "{id}/monthly-stats/{year:int}/{month:int}");
    }

    [EndpointSummary("Get Storage Accounts")]
    [EndpointDescription("Lấy tài khoản lưu trữ R2 duy nhất của người dùng kèm dung lượng đã dùng, số video upload và thống kê request theo tháng")]
    public static async Task<Ok<List<StorageAccountDto>>> GetStorageAccounts(ISender sender)
        => TypedResults.Ok(await sender.Send(new GetStorageAccountsQuery()));

    [EndpointSummary("Get Storage Account by Id")]
    [EndpointDescription("Lấy thông tin tài khoản lưu trữ R2 (không trả Secret Access Key)")]
    public static async Task<Ok<StorageAccountDto>> GetStorageAccountById(ISender sender, int id)
        => TypedResults.Ok(await sender.Send(new GetStorageAccountByIdQuery(id)));

    [EndpointSummary("Create Storage Account")]
    [EndpointDescription("Thiết lập tài khoản Cloudflare R2 duy nhất (Account ID, khoá truy cập, bucket, địa chỉ công khai, hạn mức, giới hạn video/tháng)")]
    public static async Task<Created<int>> CreateStorageAccount(ISender sender, CreateStorageAccountCommand command)
    {
        var id = await sender.Send(command);

        return TypedResults.Created($"/{nameof(StorageAccounts)}/{id}", id);
    }

    [EndpointSummary("Update Storage Account")]
    [EndpointDescription("Sửa tài khoản lưu trữ R2; bỏ trống Secret Access Key để giữ khoá đang lưu")]
    public static async Task<Results<NoContent, BadRequest>> UpdateStorageAccount(ISender sender, int id,
        UpdateStorageAccountCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();

        await sender.Send(command);

        return TypedResults.NoContent();
    }

    [EndpointSummary("Delete Storage Account")]
    [EndpointDescription("Xoá tài khoản lưu trữ khỏi hệ thống; còn tập phim lưu video ở đó thì không xoá được")]
    public static async Task<NoContent> DeleteStorageAccount(ISender sender, int id)
    {
        await sender.Send(new DeleteStorageAccountCommand(id));

        return TypedResults.NoContent();
    }

    [EndpointSummary("Check Storage Account")]
    [EndpointDescription("Kết nối thử tới bucket, đặt lại dung lượng đã dùng theo tổng kích thước file thực có trong bucket và ghi nhận request ListObjects")]
    public static async Task<Ok<CheckStorageAccountResult>> CheckStorageAccount(ISender sender, int id)
        => TypedResults.Ok(await sender.Send(new CheckStorageAccountCommand(id)));

    public sealed record TrimStorageAccountRequest(bool Apply, bool DeleteOrphans);

    [EndpointSummary("Trim Storage Account")]
    [EndpointDescription("Đưa dung lượng bucket về dưới hạn mức: gỡ video của các tập (phim ít lượt xem trước, tập cuối trước), tuỳ chọn xoá file mồ côi. apply = false chỉ trả kế hoạch, không xoá")]
    public static async Task<Ok<TrimStorageAccountResult>> TrimStorageAccount(ISender sender, int id, TrimStorageAccountRequest request)
        => TypedResults.Ok(await sender.Send(new TrimStorageAccountCommand(id, request.Apply, request.DeleteOrphans)));

    public sealed record UpdateMonthlyStatRequest(
        int VideosUploaded,
        long BytesUploaded,
        int VideosDeleted,
        long BytesDeleted,
        long PutObjectRequests,
        long ListObjectsRequests,
        long OtherClassARequests,
        long GetObjectRequests,
        long HeadObjectRequests,
        long OtherClassBRequests,
        long DeleteObjectRequests);

    [EndpointSummary("Update Storage Account Monthly Stat")]
    [EndpointDescription("Đối soát / cập nhật số lượng video và số request theo loại (Class A, Class B, Delete) của một tháng")]
    public static async Task<Ok<StorageAccountMonthlyStatDto>> UpdateStorageAccountMonthlyStat(
        ISender sender, int id, int year, int month, UpdateMonthlyStatRequest request)
        => TypedResults.Ok(await sender.Send(new UpdateStorageAccountMonthlyStatCommand
        {
            StorageAccountId = id,
            Year = year,
            Month = month,
            VideosUploaded = request.VideosUploaded,
            BytesUploaded = request.BytesUploaded,
            VideosDeleted = request.VideosDeleted,
            BytesDeleted = request.BytesDeleted,
            PutObjectRequests = request.PutObjectRequests,
            ListObjectsRequests = request.ListObjectsRequests,
            OtherClassARequests = request.OtherClassARequests,
            GetObjectRequests = request.GetObjectRequests,
            HeadObjectRequests = request.HeadObjectRequests,
            OtherClassBRequests = request.OtherClassBRequests,
            DeleteObjectRequests = request.DeleteObjectRequests
        }));
}
