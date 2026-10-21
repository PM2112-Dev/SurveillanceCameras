using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Episodes.Queries.DTOs;

/// <summary>Chi tiết chương, gồm nội dung gốc và nội dung đã biên tập.</summary>
public class EpisodeDto : EpisodeBriefDto
{
    public string? TytChapterId { get; init; }

    public string? ContentRaw { get; init; }

    public string? ContentEdit { get; init; }

    public string? PromptTitle { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Episode, EpisodeDto>()
                .IncludeBase<Episode, EpisodeBriefDto>()
                .ForMember(dest => dest.PromptTitle, opt => opt.MapFrom(src => src.Prompt!.Title))
                .ForMember(dest => dest.RawLength, opt => opt.MapFrom(src => src.ContentRaw!.Length))
                .ForMember(dest => dest.EditLength, opt => opt.MapFrom(src => src.ContentEdit!.Length));
        }
    }
}

/// <summary>Chương trong danh sách — không kèm nội dung (truyện có thể có hàng nghìn chương).</summary>
public class EpisodeBriefDto : BaseDto
{
    public int WorkId { get; init; }

    public int EpisodeNumber { get; init; }

    public string? LinkRaw { get; init; }

    public string? NameRaw { get; init; }

    public string? NameEdit { get; init; }

    public EpisodeStatus Status { get; init; }

    public string? Error { get; init; }

    public int? PromptId { get; init; }

    /// <summary>Số ký tự nội dung gốc / đã biên tập (để biết chương có nội dung chưa, không tải cả nội dung).</summary>
    public int? RawLength { get; init; }

    public int? EditLength { get; init; }

    public DateTimeOffset? FetchedAt { get; init; }

    public DateTimeOffset? EditedAt { get; init; }

    public bool IsUploaded { get; init; }

    public DateTimeOffset? TytUploadedAt { get; init; }

    /// <summary>Lỗi của lần đăng lên TYT gần nhất.</summary>
    public string? TytUploadError { get; init; }

    public bool IsPublished { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Episode, EpisodeBriefDto>()
                .ForMember(dest => dest.RawLength, opt => opt.MapFrom(src => src.ContentRaw!.Length))
                .ForMember(dest => dest.EditLength, opt => opt.MapFrom(src => src.ContentEdit!.Length));
        }
    }
}
