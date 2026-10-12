using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.Languages;

public class LanguageDto : BaseDto
{
    /// <summary>Số phim/truyện thuộc ngôn ngữ này.</summary>
    public int WorkCount { get; init; }

    /// <summary>Số chương/tập thuộc ngôn ngữ này.</summary>
    public int EpisodeCount { get; init; }
}

internal static class LanguageQueries
{
    public static IQueryable<LanguageDto> ToDto(this IQueryable<Language> languages,
        Common.Interfaces.IApplicationDbContext context)
        => languages.Select(x => new LanguageDto
        {
            Id = x.Id,
            Title = x.Title,
            Code = x.Code,
            BaseStatus = x.BaseStatus.ToString(),
            WorkCount = context.Works.Count(w => w.LanguageId == x.Id),
            EpisodeCount = context.Episodes.Count(e => e.LanguageId == x.Id)
        });
}
