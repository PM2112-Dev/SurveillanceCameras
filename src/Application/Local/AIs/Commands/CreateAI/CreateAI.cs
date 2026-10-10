using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.AIs.Commands.CreateAI;

public record CreateAICommand : IRequest<int>
{
    public string? Title { get; init; }

    public AIProvider Provider { get; init; }

    public string? Model { get; init; }

    public string? ApiKey { get; init; }

    public string? Project { get; init; }

    public string? Email { get; init; }
}

public class CreateAICommandValidator : AbstractValidator<CreateAICommand>
{
    public CreateAICommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Provider).IsInEnum();
        RuleFor(x => x.Model).NotEmpty().MaximumLength(AIValidation.MaxModelLength);
        RuleFor(x => x.ApiKey).NotEmpty().MaximumLength(AIValidation.MaxApiKeyLength);
        RuleFor(x => x.Project).MaximumLength(200);
        RuleFor(x => x.Email).MaximumLength(320)
            .Must(AIValidation.BeValidEmail).WithMessage("Email không hợp lệ.");
    }
}

public class CreateAICommandHandler : IRequestHandler<CreateAICommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateAICommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateAICommand request, CancellationToken cancellationToken)
    {
        var entity = new AI
        {
            Title = request.Title,
            Provider = request.Provider,
            Model = request.Model?.Trim(),
            ApiKey = request.ApiKey?.Trim(),
            Project = AIValidation.NullIfEmpty(request.Project),
            Email = AIValidation.NullIfEmpty(request.Email)
        };

        _context.AIs.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
