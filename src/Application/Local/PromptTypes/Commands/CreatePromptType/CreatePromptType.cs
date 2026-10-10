using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.PromptTypes.Commands.CreatePromptType;

public record CreatePromptTypeCommand : IRequest<int>
{
    public string? Title { get; init; }

    /// <summary>Mã tác vụ, vd. <c>edit-description</c> (xem <c>PromptTypeCodes</c>).</summary>
    public string? Code { get; init; }

    /// <summary>Khi nào dùng loại prompt này.</summary>
    public string? Description { get; init; }
}

public class CreatePromptTypeCommandValidator : AbstractValidator<CreatePromptTypeCommand>
{
    public CreatePromptTypeCommandValidator(IApplicationDbContext context, IUser user)
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);

        // Code là mã hệ thống dùng để chọn prompt theo tác vụ (xem PromptTypeCodes) nên phải duy nhất.
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(100)
            .Matches("^[a-zA-Z0-9._-]+$").WithMessage("Mã chỉ gồm chữ, số và các ký tự . _ -")
            .MustAsync((command, code, ct) => PromptTypeValidation.IsCodeAvailableAsync(context, user, code, null, ct))
            .WithMessage("Mã loại prompt '{PropertyValue}' đã tồn tại.");

        RuleFor(x => x.Description).MaximumLength(2000);
    }
}

public class CreatePromptTypeCommandHandler : IRequestHandler<CreatePromptTypeCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreatePromptTypeCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreatePromptTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = new PromptType
        {
            Title = request.Title,
            Code = PromptTypeValidation.NormalizeCode(request.Code),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()
        };

        _context.PromptTypes.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
