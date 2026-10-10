using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.UserProfileTypes.Commands.UpdateUserProfileType;

public record UpdateUserProfileTypeCommand : IRequest
{
    public int Id { get; init; }

    public string? Title { get; init; }

    public string? Code { get; init; }
}

public class UpdateUserProfileTypeCommandValidator : AbstractValidator<UpdateUserProfileTypeCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateUserProfileTypeCommandValidator(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;

        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);

        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(100)
            .MustAsync(BeUniqueCode).WithMessage("Mã loại profile '{PropertyValue}' đã tồn tại.");
    }

    private async Task<bool> BeUniqueCode(UpdateUserProfileTypeCommand command, string? code,
        CancellationToken cancellationToken)
    {
        var normalized = code?.Trim().ToLower();

        return !await _context.UserProfileTypes
            .AnyAsync(x => x.CreatedBy == _user.Id && x.Id != command.Id && x.Code == normalized, cancellationToken);
    }
}

public class UpdateUserProfileTypeCommandHandler : IRequestHandler<UpdateUserProfileTypeCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateUserProfileTypeCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(UpdateUserProfileTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.UserProfileTypes
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        entity.Title = request.Title;
        entity.Code = request.Code?.Trim().ToLower();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
