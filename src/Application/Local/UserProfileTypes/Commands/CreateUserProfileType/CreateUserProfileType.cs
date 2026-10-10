using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.UserProfileTypes.Commands.CreateUserProfileType;

public record CreateUserProfileTypeCommand : IRequest<int>
{
    public string? Title { get; init; }

    public string? Code { get; init; }
}

public class CreateUserProfileTypeCommandValidator : AbstractValidator<CreateUserProfileTypeCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public CreateUserProfileTypeCommandValidator(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;

        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);

        // Code là khoá nhận diện nền tảng (xem UserProfileTypeCodes) nên phải duy nhất.
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(100)
            .MustAsync(BeUniqueCode).WithMessage("Mã loại profile '{PropertyValue}' đã tồn tại.");
    }

    private async Task<bool> BeUniqueCode(string? code, CancellationToken cancellationToken)
    {
        var normalized = code?.Trim().ToLower();

        return !await _context.UserProfileTypes
            .AnyAsync(x => x.CreatedBy == _user.Id && x.Code == normalized, cancellationToken);
    }
}

public class CreateUserProfileTypeCommandHandler : IRequestHandler<CreateUserProfileTypeCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateUserProfileTypeCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateUserProfileTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = new UserProfileType { Title = request.Title, Code = request.Code?.Trim().ToLower() };

        _context.UserProfileTypes.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
