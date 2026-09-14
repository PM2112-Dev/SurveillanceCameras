using SurveillanceCameras.Application.Local.WebSources.Queries.DTOs;
using SurveillanceCameras.Application.Repositories;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.WebSources.Queries.GetWebSourceById;

public record GetWebSourceIdQuery(int Id) : IRequest<WebSourceDto>;

public class GetWebSourceByIdQueryHandler : IRequestHandler<GetWebSourceIdQuery, WebSourceDto>
{
    private readonly IWebSourceRepository _repository;
    private readonly IMapper _mapper;
    
    public GetWebSourceByIdQueryHandler(IWebSourceRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<WebSourceDto> Handle(GetWebSourceIdQuery request, CancellationToken cancellationToken)
    {
        var query = await _repository.GetByIdAsync(request.Id, cancellationToken);
        
        var entity = _mapper.Map<WebSourceDto>(query);

        return entity;
    }
}
