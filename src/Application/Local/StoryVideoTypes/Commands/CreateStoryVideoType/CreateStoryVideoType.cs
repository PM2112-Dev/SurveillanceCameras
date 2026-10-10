using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.StoryVideoTypes.Commands.CreateStoryVideoType;

public record CreateStoryVideoTypeCommand : IRequest<int>
{
    public string? Title { get; init; }

    public string? Code { get; init; }
}

public class CreateStoryVideoTypeCommandValidator : AbstractValidator<CreateStoryVideoTypeCommand>
{
    public CreateStoryVideoTypeCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
    }
}

public class CreateStoryVideoTypeCommandHandler : IRequestHandler<CreateStoryVideoTypeCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateStoryVideoTypeCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateStoryVideoTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = new StoryVideoType { Title = request.Title, Code = request.Code };

        _context.StoryVideoTypes.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
