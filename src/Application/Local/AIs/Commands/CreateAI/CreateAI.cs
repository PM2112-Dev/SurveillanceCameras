using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.AIs.Commands.CreateAI;

public record CreateAICommand : IRequest<int>
{
    public string? Title { get; init; }
    public string? Model { get; init; }
    public string? ApiKey { get; init; }
}

public class CreateAICommandValidator : AbstractValidator<CreateAICommand>
{
    public CreateAICommandValidator()
    {
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
        var entity = new AI { Title = request.Title, Model = request.Model, ApiKey = request.ApiKey };
        
        _context.AIs.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
