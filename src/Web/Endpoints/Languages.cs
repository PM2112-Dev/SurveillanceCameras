using Microsoft.AspNetCore.Http.HttpResults;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.Languages;
using SurveillanceCameras.Application.Local.Languages.Commands;
using SurveillanceCameras.Application.Local.Languages.Queries;

namespace SurveillanceCameras.Web.Endpoints;

/// <summary>Ngôn ngữ của phim/tập. Đồng bộ drama TikTok tự gán theo <c>textLanguage</c> và tự thêm mã chưa khai báo.</summary>
public class Languages : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetLanguages);
        groupBuilder.MapGet(GetLanguageById, "{id}");
        groupBuilder.MapPost(CreateLanguage);
        groupBuilder.MapPut(UpdateLanguage, "{id}");
        groupBuilder.MapDelete(DeleteLanguage, "{id}");
    }

    [EndpointSummary("Get Languages")]
    [EndpointDescription("Lấy danh sách ngôn ngữ (tìm theo tên/mã, có phân trang) kèm số phim và số tập")]
    public static async Task<Ok<PaginatedList<LanguageDto>>> GetLanguages(ISender sender, string? title = null,
        int page = 1, int pageSize = 20)
        => TypedResults.Ok(await sender.Send(new GetLanguagesQuery { Title = title, Page = page, PageSize = pageSize }));

    [EndpointSummary("Get Language by Id")]
    [EndpointDescription("Lấy một ngôn ngữ theo ID")]
    public static async Task<Ok<LanguageDto>> GetLanguageById(ISender sender, int id)
        => TypedResults.Ok(await sender.Send(new GetLanguageByIdQuery(id)));

    [EndpointSummary("Create Language")]
    [EndpointDescription("Khai báo ngôn ngữ mới (mã không trùng)")]
    public static async Task<Created<int>> CreateLanguage(ISender sender, CreateLanguageCommand command)
    {
        var id = await sender.Send(command);

        return TypedResults.Created($"/{nameof(Languages)}/{id}", id);
    }

    [EndpointSummary("Update Language")]
    [EndpointDescription("Sửa tên/mã ngôn ngữ")]
    public static async Task<Results<NoContent, BadRequest>> UpdateLanguage(ISender sender, int id, UpdateLanguageCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();

        await sender.Send(command);

        return TypedResults.NoContent();
    }

    [EndpointSummary("Delete Language")]
    [EndpointDescription("Xoá ngôn ngữ (phim và tập đang dùng sẽ bị gỡ liên kết)")]
    public static async Task<NoContent> DeleteLanguage(ISender sender, int id)
    {
        await sender.Send(new DeleteLanguageCommand(id));

        return TypedResults.NoContent();
    }
}
