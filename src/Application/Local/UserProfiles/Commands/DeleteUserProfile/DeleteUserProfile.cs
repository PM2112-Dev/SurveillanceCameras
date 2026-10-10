using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.UserProfiles.Commands.DeleteUserProfile;

public record DeleteUserProfileCommand(int Id) : IRequest;

public class DeleteUserProfileCommandValidator : AbstractValidator<DeleteUserProfileCommand>
{
    public DeleteUserProfileCommandValidator()
    {
    }
}

public class DeleteUserProfileCommandHandler : IRequestHandler<DeleteUserProfileCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DeleteUserProfileCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(DeleteUserProfileCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.UserProfiles
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        _context.UserProfiles.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
