using Microsoft.AspNetCore.Http.HttpResults;
using SurveillanceCameras.Application.Common.Models;
using SurveillanceCameras.Application.Local.Categories.Commands.CreateCategory;
using SurveillanceCameras.Application.Local.Categories.Commands.DeleteCategory;
using SurveillanceCameras.Application.Local.Categories.Commands.UpdateCategory;
using SurveillanceCameras.Application.Local.Categories.Queries.DTOs;
using SurveillanceCameras.Application.Local.Categories.Queries.GetCategories;
using SurveillanceCameras.Application.Local.Categories.Queries.GetCategoryById;

namespace SurveillanceCameras.Web.Endpoints;

public class Categories : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetCategories);
        groupBuilder.MapGet(GetCategoryById, "{id}");
        groupBuilder.MapPost(CreateCategory);
        groupBuilder.MapPut(UpdateCategory, "{id}");
        groupBuilder.MapDelete(DeleteCategory, "{id}");
    }

    [EndpointSummary("Get Categories")]
    [EndpointDescription("Lấy danh sách Category (lọc theo tên, có phân trang)")]
    public static async Task<Ok<PaginatedList<CategoryDto>>> GetCategories(
        ISender sender, [AsParameters] GetCategoriesQuery query)
    {
        var vm = await sender.Send(query);

        return TypedResults.Ok(vm);
    }

    [EndpointSummary("Get Category by Id")]
    [EndpointDescription("Lấy một Category theo ID")]
    public static async Task<Ok<CategoryDto>> GetCategoryById(ISender sender, int id)
    {
        var entity = await sender.Send(new GetCategoryByIdQuery(id));

        return TypedResults.Ok(entity);
    }

    [EndpointSummary("Create Category")]
    [EndpointDescription("Tạo mới Category")]
    public static async Task<Created<int>> CreateCategory(ISender sender, CreateCategoryCommand command)
    {
        var id = await sender.Send(command);

        return TypedResults.Created($"/{nameof(Categories)}/{id}", id);
    }

    [EndpointSummary("Update Category")]
    [EndpointDescription("Cập nhật Category")]
    public static async Task<Results<NoContent, BadRequest>> UpdateCategory(ISender sender, int id, UpdateCategoryCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();

        await sender.Send(command);

        return TypedResults.NoContent();
    }

    [EndpointSummary("Delete Category")]
    [EndpointDescription("Xoá Category")]
    public static async Task<NoContent> DeleteCategory(ISender sender, int id)
    {
        await sender.Send(new DeleteCategoryCommand(id));

        return TypedResults.NoContent();
    }
}
