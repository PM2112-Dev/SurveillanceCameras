using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Common.Models.TikTok;
using SurveillanceCameras.Application.Repository.TikTok;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Movies.Queries.GetChannelDramas;

/// <summary>Một trang drama của kênh TikTok đã lưu (đọc thẳng từ TikTok), kèm phim đã đồng bộ tương ứng.</summary>
public record GetChannelDramasQuery : IRequest<ChannelDramasDto>
{
    public int UserProfileId { get; init; }

    public string Cursor { get; init; } = "0";

    public int Count { get; init; } = 20;
}

public record ChannelDramasDto
{
    public string? Cursor { get; init; }

    public bool HasMore { get; init; }

    public List<DramaDto> Items { get; init; } = [];

    /// <summary>dramaID → Id phim trong hệ thống, cho các drama đã đồng bộ.</summary>
    public Dictionary<string, int> MovieIds { get; init; } = [];
}

public class GetChannelDramasQueryValidator : AbstractValidator<GetChannelDramasQuery>
{
    public GetChannelDramasQueryValidator()
    {
        RuleFor(x => x.Count).InclusiveBetween(1, 50);
        RuleFor(x => x.Cursor).NotEmpty();
    }
}

public class GetChannelDramasQueryHandler : IRequestHandler<GetChannelDramasQuery, ChannelDramasDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly ITikTokRepository _tikTok;

    public GetChannelDramasQueryHandler(IApplicationDbContext context, IUser user, ITikTokRepository tikTok)
    {
        _context = context;
        _user = user;
        _tikTok = tikTok;
    }

    public async Task<ChannelDramasDto> Handle(GetChannelDramasQuery request, CancellationToken cancellationToken)
    {
        var channel = await TikTokChannel.LoadAsync(_context, _user.Id, request.UserProfileId, cancellationToken);

        var page = await _tikTok.FetchDramaList(null, channel.SecUid!, request.Count, request.Cursor, cancellationToken);
        var ids = page.DramaList.Select(x => x.DramaId).OfType<string>().ToList();

        var movieIds = await _context.Works
            .Where(x => x.CreatedBy == _user.Id && x.Type == WorkType.Movie && x.UserProfileId == channel.Id
                        && ids.Contains(x.ExternalId!))
            .Select(x => new { x.ExternalId, x.Id })
            .ToDictionaryAsync(x => x.ExternalId!, x => x.Id, cancellationToken);

        return new ChannelDramasDto
        {
            Cursor = page.Cursor,
            HasMore = page.HasMore,
            Items = page.DramaList,
            MovieIds = movieIds
        };
    }
}
