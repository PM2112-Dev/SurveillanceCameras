using FluentValidation;
using MediatR;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.WebSources.IntegrationEvents;
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
        var entity = new WebSource { Title = request.Title, BaseUrl = request.BaseUrl, };

        await _context.ExecuteInTransactionAsync(async ct =>
        {
            _context.WebSources.Add(entity);
            await _context.SaveChangesAsync(ct);

            entity.AddIntegrationEvent(
                new FetchStoryWebSourceIntegrationEvent
                {
                    WebSourceId = entity.Id, 
                    LinkRaw = entity.BaseUrl
                }, ct);
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
