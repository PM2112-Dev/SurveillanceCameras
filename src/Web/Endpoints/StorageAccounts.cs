using Microsoft.AspNetCore.Http.HttpResults;
using SurveillanceCameras.Application.Local.StorageAccounts.Commands.CheckStorageAccount;
using SurveillanceCameras.Application.Local.StorageAccounts.Commands.CreateStorageAccount;
using SurveillanceCameras.Application.Local.StorageAccounts.Commands.DeleteStorageAccount;
using SurveillanceCameras.Application.Local.StorageAccounts.Commands.TrimStorageAccount;
using SurveillanceCameras.Application.Local.StorageAccounts.Commands.UpdateStorageAccount;
using SurveillanceCameras.Application.Local.StorageAccounts.Queries.DTOs;
using SurveillanceCameras.Application.Local.StorageAccounts.Queries.GetStorageAccountById;
using SurveillanceCameras.Application.Local.StorageAccounts.Queries.GetStorageAccounts;

namespace SurveillanceCameras.Web.Endpoints;

/// <summary>Tài khoản Cloudflare R2 dùng để lưu file video của tập phim.</summary>
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
    }

    [EndpointSummary("Get Storage Accounts")]
    [EndpointDescription("Mọi tài khoản lưu trữ video kèm dung lượng đã dùng, theo thứ tự được dùng để lưu")]
    public static async Task<Ok<List<StorageAccountDto>>> GetStorageAccounts(ISender sender)
        => TypedResults.Ok(await sender.Send(new GetStorageAccountsQuery()));

    [EndpointSummary("Get Storage Account by Id")]
    [EndpointDescription("Lấy một tài khoản lưu trữ (không trả Secret Access Key)")]
    public static async Task<Ok<StorageAccountDto>> GetStorageAccountById(ISender sender, int id)
        => TypedResults.Ok(await sender.Send(new GetStorageAccountByIdQuery(id)));

    [EndpointSummary("Create Storage Account")]
    [EndpointDescription("Thêm tài khoản Cloudflare R2 (Account ID, khoá truy cập, bucket, địa chỉ công khai, hạn mức)")]
    public static async Task<Created<int>> CreateStorageAccount(ISender sender, CreateStorageAccountCommand command)
    {
        var id = await sender.Send(command);

        return TypedResults.Created($"/{nameof(StorageAccounts)}/{id}", id);
    }

    [EndpointSummary("Update Storage Account")]
    [EndpointDescription("Sửa tài khoản lưu trữ; bỏ trống Secret Access Key để giữ khoá đang lưu")]
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
    [EndpointDescription("Kết nối thử tới bucket và đặt lại dung lượng đã dùng theo tổng kích thước file thực có trong bucket")]
    public static async Task<Ok<CheckStorageAccountResult>> CheckStorageAccount(ISender sender, int id)
        => TypedResults.Ok(await sender.Send(new CheckStorageAccountCommand(id)));

    public sealed record TrimStorageAccountRequest(bool Apply, bool DeleteOrphans);

    [EndpointSummary("Trim Storage Account")]
    [EndpointDescription("Đưa dung lượng bucket về dưới hạn mức: gỡ video của các tập (phim ít lượt xem trước, tập cuối trước), tuỳ chọn xoá file mồ côi. apply = false chỉ trả kế hoạch, không xoá")]
    public static async Task<Ok<TrimStorageAccountResult>> TrimStorageAccount(ISender sender, int id, TrimStorageAccountRequest request)
        => TypedResults.Ok(await sender.Send(new TrimStorageAccountCommand(id, request.Apply, request.DeleteOrphans)));
}
