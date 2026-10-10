using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.UserProfileTypes.Commands.DeleteUserProfileType;

public record DeleteUserProfileTypeCommand(int Id) : IRequest;

public class DeleteUserProfileTypeCommandValidator : AbstractValidator<DeleteUserProfileTypeCommand>
{
    public DeleteUserProfileTypeCommandValidator()
    {
    }
}

public class DeleteUserProfileTypeCommandHandler : IRequestHandler<DeleteUserProfileTypeCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DeleteUserProfileTypeCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(DeleteUserProfileTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.UserProfileTypes
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        _context.UserProfileTypes.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
