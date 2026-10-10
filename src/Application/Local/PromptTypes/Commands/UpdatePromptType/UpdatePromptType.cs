using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Application.Local.PromptTypes.Commands.UpdatePromptType;

public record UpdatePromptTypeCommand : IRequest
{
    public int Id { get; init; }

    public string? Title { get; init; }

    public string? Code { get; init; }

    public string? Description { get; init; }
}

public class UpdatePromptTypeCommandValidator : AbstractValidator<UpdatePromptTypeCommand>
{
    public UpdatePromptTypeCommandValidator(IApplicationDbContext context, IUser user)
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);

        // Code là mã hệ thống dùng để chọn prompt theo tác vụ (xem PromptTypeCodes) nên phải duy nhất.
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(100)
            .Matches("^[a-zA-Z0-9._-]+$").WithMessage("Mã chỉ gồm chữ, số và các ký tự . _ -")
            .MustAsync((command, code, ct) => PromptTypeValidation.IsCodeAvailableAsync(context, user, code, command.Id, ct))
            .WithMessage("Mã loại prompt '{PropertyValue}' đã tồn tại.");

        RuleFor(x => x.Description).MaximumLength(2000);
    }
}

public class UpdatePromptTypeCommandHandler : IRequestHandler<UpdatePromptTypeCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdatePromptTypeCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(UpdatePromptTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.PromptTypes
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id,
            cancellationToken);

        Guard.Against.NotFound(request.Id, entity);
        
        entity.Title = request.Title;
        entity.Code = PromptTypeValidation.NormalizeCode(request.Code);
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        
        await _context.SaveChangesAsync(cancellationToken);
    }
}
