using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.AIs.Commands.UpdateAI;

public record UpdateAICommand : IRequest
{
    public int Id { get; set; }
    public string? Title { get; init; }
    public string? Model { get; init; }
    public string? ApiKey { get; init; }
}

public class UpdateAICommandValidator : AbstractValidator<UpdateAICommand>
{
    public UpdateAICommandValidator()
    {
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
            .FirstOrDefaultAsync(x=> x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);
        
        Guard.Against.NotFound(request.Id, entity);
        
        entity.Title = request.Title;
        entity.Model = request.Model;
        entity.ApiKey = request.ApiKey;
        
        await _context.SaveChangesAsync(cancellationToken);
    }
}
