using Microsoft.AspNetCore.Http.HttpResults;
using SurveillanceCameras.Application.Cameras.Commands.CreateCamera;
using SurveillanceCameras.Application.Cameras.Commands.DeleteCamera;
using SurveillanceCameras.Application.Cameras.Commands.UpdateCamera;
using SurveillanceCameras.Application.Cameras.Queries.GetCameraById;
using SurveillanceCameras.Application.Cameras.Queries.GetCameras;
using SurveillanceCameras.Application.Cameras.Queries.Model;
using SurveillanceCameras.Application.Local.Cameras.Queries.GetCameraById;

namespace SurveillanceCameras.Web.Endpoints;

public class Cameras : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetCameras);
        groupBuilder.MapGet(GetCameraById, "{id}");
        groupBuilder.MapPost(CreateCamera);
        groupBuilder.MapPut(UpdateCamera, "{id}");
        groupBuilder.MapDelete(DeleteCamera, "{id}");
    }
    
    [EndpointSummary("Get all cameras")]
    [EndpointDescription("Lấy toàn bộ danh sách camera theo user hiện tại")]
    public static async Task<Ok<CameraVm>> GetCameras(ISender sender)
    {
        var vm = await sender.Send(new GetCamerasQuery());

        return TypedResults.Ok(vm);
    }

    [EndpointSummary("Get camera by id")]
    [EndpointDescription("Lấy thông tin camera theo id, chỉ lấy được camera của user hiện tại")]
    public static async Task<Ok<CameraDto>> GetCameraById(ISender sender, int id)
    {
        var camera = await sender.Send(new GetCameraByIdQuery(id));

        return TypedResults.Ok(camera);
    }
    
    [EndpointSummary("Create a camera")]
    [EndpointDescription("Tạo mới một camera với thông tin được cung cấp, trả về id của camera vừa tạo")]
    public static async Task<Created<int>> CreateCamera(ISender sender, CreateCameraCommand command)
    {
        var id = await sender.Send(command);
        
        return TypedResults.Created($"/{nameof(Cameras)}/{id}", id);
    }

    [EndpointSummary("Update a camera")]
    [EndpointDescription("Cập nhật thông tin một camera, id trong URL phải trùng với id trong payload")]
    public static async Task<Results<NoContent, BadRequest>> UpdateCamera(ISender sender, int id,
        UpdateCameraCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();
        
        await sender.Send(command);
        
        return TypedResults.NoContent();
    }

    [EndpointSummary("Delete a camera")]
    [EndpointDescription("Xóa một camera, chỉ xóa được camera của user hiện tại")]
    public static async Task<NoContent> DeleteCamera(ISender sender, int id)
    {
        await sender.Send(new DeleteCameraCommand(id));
        
        return TypedResults.NoContent();
    }
}
