using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.PromptTypes.Commands.CreatePromptType;

public record CreatePromptTypeCommand : IRequest<int>
{
    public string? Title { get; set; }
}

public class CreatePromptTypeCommandValidator : AbstractValidator<CreatePromptTypeCommand>
{
    public CreatePromptTypeCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required");
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
        var entity = new PromptType { Title = request.Title };

        _context.PromptTypes.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
