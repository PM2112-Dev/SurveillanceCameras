using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Movies.Commands.SetMovieAccess;

/// <summary>
/// Đặt quyền xem trên app cho các tập của một phim: khoảng tập mở bằng quảng cáo và khoảng tập chỉ dành cho VIP. Tập ngoài
/// hai khoảng thì miễn phí; tập nằm trong cả hai thì là VIP. <see cref="UseDefault"/> bật thì bỏ cấu hình riêng, phim
/// quay về mặc định của hệ thống. Cấu hình riêng mà không bật khoảng nào nghĩa là mọi tập đều miễn phí.
/// <see cref="IsVipOnly"/> đặt cả phim chỉ dành cho VIP, độc lập với các khoảng tập.
/// </summary>
public record SetMovieAccessCommand : IRequest
{
    public int MovieId { get; init; }

    public bool UseDefault { get; init; }

    /// <summary>Cả phim chỉ dành cho VIP (app gắn nhãn VIP lên phim).</summary>
    public bool IsVipOnly { get; init; }

    /// <summary>Tập đầu của khoảng quảng cáo; trống là phim không có tập quảng cáo.</summary>
    public int? AdFromEpisode { get; init; }

    /// <summary>Tập cuối của khoảng quảng cáo; trống là tới hết phim.</summary>
    public int? AdToEpisode { get; init; }

    /// <summary>Tập đầu của khoảng VIP; trống là phim không có tập VIP.</summary>
    public int? VipFromEpisode { get; init; }

    /// <summary>Tập cuối của khoảng VIP; trống là tới hết phim.</summary>
    public int? VipToEpisode { get; init; }
}

public class SetMovieAccessCommandValidator : AbstractValidator<SetMovieAccessCommand>
{
    public SetMovieAccessCommandValidator()
    {
        When(x => !x.UseDefault, () =>
        {
            Range(x => x.AdFromEpisode, x => x.AdToEpisode, "quảng cáo");
            Range(x => x.VipFromEpisode, x => x.VipToEpisode, "VIP");
        });
    }

    private void Range(Func<SetMovieAccessCommand, int?> from, Func<SetMovieAccessCommand, int?> to, string name)
    {
        RuleFor(x => x)
            .Must(x => from(x) is null or >= 1).WithMessage($"Tập bắt đầu {name} phải từ 1 trở lên.")
            .Must(x => to(x) is null || from(x) is not null).WithMessage($"Nhập tập bắt đầu {name} trước khi nhập tập kết thúc.")
            .Must(x => to(x) is null || from(x) is null || to(x) >= from(x))
            .WithMessage($"Tập kết thúc {name} không được nhỏ hơn tập bắt đầu.");
    }
}

public class SetMovieAccessCommandHandler : IRequestHandler<SetMovieAccessCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public SetMovieAccessCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(SetMovieAccessCommand request, CancellationToken cancellationToken)
    {
        var movie = await _context.Works.FirstOrDefaultAsync(
            x => x.Id == request.MovieId && x.CreatedBy == _user.Id && x.Type == WorkType.Movie, cancellationToken);

        Guard.Against.NotFound(request.MovieId, movie);

        movie.HasCustomAccess = !request.UseDefault;
        movie.IsVipOnly = request.IsVipOnly;
        movie.AdFromEpisode = request.UseDefault ? null : request.AdFromEpisode;
        movie.AdToEpisode = request.UseDefault ? null : request.AdToEpisode;
        movie.VipFromEpisode = request.UseDefault ? null : request.VipFromEpisode;
        movie.VipToEpisode = request.UseDefault ? null : request.VipToEpisode;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
