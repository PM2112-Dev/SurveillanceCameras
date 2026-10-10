using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.AIs.Commands.UpdateAI;

public record UpdateAICommand : IRequest
{
    public int Id { get; init; }

    public string? Title { get; init; }

    public AIProvider Provider { get; init; }

    public string? Model { get; init; }

    /// <summary>Bỏ trống để giữ API key đang lưu (giao diện không được xem lại key cũ).</summary>
    public string? ApiKey { get; init; }

    public string? Project { get; init; }

    public string? Email { get; init; }
}

public class UpdateAICommandValidator : AbstractValidator<UpdateAICommand>
{
    public UpdateAICommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Provider).IsInEnum();
        RuleFor(x => x.Model).NotEmpty().MaximumLength(AIValidation.MaxModelLength);
        RuleFor(x => x.ApiKey).MaximumLength(AIValidation.MaxApiKeyLength);
        RuleFor(x => x.Project).MaximumLength(200);
        RuleFor(x => x.Email).MaximumLength(320)
            .Must(AIValidation.BeValidEmail).WithMessage("Email không hợp lệ.");
    }
}

public class UpdateAICommandHandler : IRequestHandler<UpdateAICommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateAICommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(UpdateAICommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.AIs
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        entity.Title = request.Title;
        entity.Provider = request.Provider;
        entity.Model = request.Model?.Trim();
        entity.Project = AIValidation.NullIfEmpty(request.Project);
        entity.Email = AIValidation.NullIfEmpty(request.Email);
        if (!string.IsNullOrWhiteSpace(request.ApiKey)) entity.ApiKey = request.ApiKey.Trim();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
