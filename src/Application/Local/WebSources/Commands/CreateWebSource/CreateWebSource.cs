using FluentValidation;
using MediatR;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Repositories;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.WebSources.Commands.CreateWebSource;

public record CreateWebSourceCommand : IRequest<int>
{
    public string? Title { get; init; }

    public string? BaseUrl { get; init; }
}

public class CreateWebSourceCommandHandler : IRequestHandler<CreateWebSourceCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateWebSourceCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateWebSourceCommand request, CancellationToken cancellationToken)
    {
        var entity = new WebSource
        {
            Title = request.Title,
            BaseUrl = request.BaseUrl,
        };
        
        _context.WebSources.Add(entity);
        
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
