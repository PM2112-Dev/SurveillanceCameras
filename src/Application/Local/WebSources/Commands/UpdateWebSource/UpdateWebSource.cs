using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.WebSources.Commands.UpdateWebSource;

public record UpdateWebSourceCommand : IRequest
{
    public int Id { get; init; }

    public string? Title { get; init; }

    public string? BaseUrl { get; init; }
}

public class UpdateWebSourceCommandHandler : IRequestHandler<UpdateWebSourceCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateWebSourceCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(UpdateWebSourceCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.WebSources.FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id,
            cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        entity.Title = request.Title;
        entity.BaseUrl = request.BaseUrl;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
