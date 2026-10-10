using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Repository.AI;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.AIs.Queries.GetProviderModels;

/// <summary>
/// Danh sách model của nền tảng mà API key dùng được. Dùng key nhập trên form, hoặc key đã lưu của
/// <see cref="AIId"/> khi đang sửa (giao diện không giữ key cũ).
/// </summary>
public record GetProviderModelsQuery : IRequest<List<AIModelDto>>
{
    public AIProvider Provider { get; init; }

    public string? ApiKey { get; init; }

    public int? AIId { get; init; }
}

public class GetProviderModelsQueryValidator : AbstractValidator<GetProviderModelsQuery>
{
    public GetProviderModelsQueryValidator()
    {
        RuleFor(x => x.Provider).IsInEnum();
        RuleFor(x => x.ApiKey)
            .NotEmpty().When(x => x.AIId is null)
            .WithMessage("Nhập API key để lấy danh sách model.");
    }
}

public class GetProviderModelsQueryHandler : IRequestHandler<GetProviderModelsQuery, List<AIModelDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IAIModelRepository _repository;

    public GetProviderModelsQueryHandler(IApplicationDbContext context, IUser user, IAIModelRepository repository)
    {
        _context = context;
        _user = user;
        _repository = repository;
    }

    public async Task<List<AIModelDto>> Handle(GetProviderModelsQuery request, CancellationToken cancellationToken)
    {
        var apiKey = request.ApiKey?.Trim();

        if (string.IsNullOrEmpty(apiKey))
        {
            var entity = await _context.AIs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.AIId && x.CreatedBy == _user.Id, cancellationToken);

            Guard.Against.NotFound(request.AIId!.Value, entity);
            apiKey = entity.ApiKey ?? "";
        }

        var models = await _repository.ListModelsAsync(request.Provider, apiKey, cancellationToken);
        return models.OrderBy(m => m.Id, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
