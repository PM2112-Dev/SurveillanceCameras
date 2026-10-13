using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shouldly;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.App;
using SurveillanceCameras.Application.Local.App.Commands;
using SurveillanceCameras.Application.Local.App.Queries;
using SurveillanceCameras.Application.Local.Movies.Commands.SetMovieAccess;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;
using SurveillanceCameras.Infrastructure.Data;

namespace SurveillanceCameras.Application.UnitTests.App;

public class AppViewerTests
{
    private const string Admin = "admin-1";
    private const string Viewer = "viewer-1";

    private static readonly IOptions<AppOptions> Options =
        Microsoft.Extensions.Options.Options.Create(new AppOptions { FreeEpisodes = 2, AdEpisodes = 1 });

    private static IUser User(string? id)
    {
        var user = new Mock<IUser>();
        user.Setup(x => x.Id).Returns(id);
        return user.Object;
    }

    /// <summary>Một phim đã xuất bản 4 tập (2 miễn phí, 1 quảng cáo, 1 VIP), một phim chưa xuất bản, một truyện.</summary>
    private static async Task<(ApplicationDbContext Db, Work Movie, Work Hidden)> SeedAsync()
    {
        var db = TestDb.Create();
        var movie = new Work
        {
            Title = "Lửa Thánh", Type = WorkType.Movie, IsPublished = true, ViewCount = 500, CreatedBy = Admin,
            Language = new Language { Title = "Tiếng Anh", Code = "en", CreatedBy = Admin },
            Episodes = Enumerable.Range(1, 4)
                .Select(n => new Episode { EpisodeNumber = n, ExternalId = $"tt{n}", CreatedBy = Admin })
                .ToList()
        };
        movie.Episodes.First().StreamUrl = "https://cdn.example.com/1.mp4";
        var hidden = new Work { Title = "Chưa xuất bản", Type = WorkType.Movie, ViewCount = 9_000, CreatedBy = Admin };
        db.Works.AddRange(movie, hidden,
            new Work { Title = "Truyện", Type = WorkType.Story, IsPublished = true, CreatedBy = Admin });
        await db.SaveChangesAsync();
        return (db, movie, hidden);
    }

    private static Task<List<AppEpisodeDto>> EpisodesAsync(ApplicationDbContext db, int movieId, string? userId)
        => new GetAppMovieEpisodesQueryHandler(db, User(userId), TimeProvider.System, Options)
            .Handle(new GetAppMovieEpisodesQuery(movieId), CancellationToken.None);

    [Test]
    public async Task Catalog_OnlyPublishedMovies_SearchIgnoresCase()
    {
        var (db, movie, hidden) = await SeedAsync();
        using var _ = db;
        var handler = new GetAppMoviesQueryHandler(db, Options);

        var all = await handler.Handle(new GetAppMoviesQuery { Sort = AppMovieSort.MostViewed }, CancellationToken.None);
        all.Items.Select(x => x.Id).ShouldBe([movie.Id]);
        all.Items.First().EpisodeCount.ShouldBe(4);

        (await handler.Handle(new GetAppMoviesQuery { Search = "  LỬA " }, CancellationToken.None)).TotalCount.ShouldBe(1);
        (await handler.Handle(new GetAppMoviesQuery { Search = "không có" }, CancellationToken.None)).TotalCount.ShouldBe(0);

        await Should.ThrowAsync<Ardalis.GuardClauses.NotFoundException>(() =>
            new GetAppMovieByIdQueryHandler(db, Options).Handle(new GetAppMovieByIdQuery(hidden.Id), CancellationToken.None));
        await Should.ThrowAsync<Ardalis.GuardClauses.NotFoundException>(() => EpisodesAsync(db, hidden.Id, null));
    }

    [Test]
    public async Task Catalog_FiltersByChosenLanguage_AndFallsBackToEnglish()
    {
        var (db, english, _) = await SeedAsync();
        using var __ = db;
        var vietnamese = new Language { Title = "Tiếng Việt", Code = "VI", CreatedBy = Admin };
        db.Works.AddRange(
            new Work { Title = "Phim Việt", Type = WorkType.Movie, IsPublished = true, Language = vietnamese, CreatedBy = Admin },
            new Work { Title = "Chưa rõ ngôn ngữ", Type = WorkType.Movie, IsPublished = true, CreatedBy = Admin },
            // Ngôn ngữ chỉ có phim chưa xuất bản thì coi như chưa có phim.
            new Work { Title = "Phim Hàn ẩn", Type = WorkType.Movie, Language = new Language { Title = "Tiếng Hàn", Code = "ko", CreatedBy = Admin }, CreatedBy = Admin });
        await db.SaveChangesAsync();
        var handler = new GetAppMoviesQueryHandler(db, Options);
        async Task<List<string?>> Titles(string? language)
            => (await handler.Handle(new GetAppMoviesQuery { Language = language }, CancellationToken.None)).Items.Select(x => x.Title).ToList();

        (await Titles("vi")).ShouldBe(["Phim Việt"]);
        (await Titles(null)).ShouldBe(["Lửa Thánh"]);
        (await Titles("ko")).ShouldBe(["Lửa Thánh"]);
        (await Titles("xx")).ShouldBe(["Lửa Thánh"]);
        (await handler.Handle(new GetAppMoviesQuery { Language = "vi" }, CancellationToken.None)).Items.Single().Language.ShouldBe("VI");

        var languages = await new GetAppLanguagesQueryHandler(db).Handle(new GetAppLanguagesQuery(), CancellationToken.None);
        languages.ShouldBe([new AppLanguageDto("en", "Tiếng Anh", 1), new AppLanguageDto("vi", "Tiếng Việt", 1)]);
    }

    [Test]
    public async Task Episodes_Guest_OnlyFreeEpisodesArePlayable()
    {
        var (db, movie, _) = await SeedAsync();
        using var __ = db;

        var episodes = await EpisodesAsync(db, movie.Id, null);

        episodes.Select(x => x.Access).ShouldBe([AppAccess.Free, AppAccess.Free, AppAccess.Ads, AppAccess.Vip]);
        // Có link tự lưu trữ thì phát bằng link đó, không thì bằng id video TikTok.
        episodes[0].Playback.ShouldBe(new AppPlaybackDto("stream", "https://cdn.example.com/1.mp4"));
        episodes[1].Playback.ShouldBe(new AppPlaybackDto("tiktok", "tt2"));
        episodes[2].Playback.ShouldBeNull();
        episodes[3].Playback.ShouldBeNull();
    }

    [Test]
    public async Task Unlock_OpensAdEpisodeForThatViewerOnly_AndRefusesVipEpisode()
    {
        var (db, movie, _) = await SeedAsync();
        using var __ = db;
        var ids = movie.Episodes!.OrderBy(x => x.EpisodeNumber).Select(x => x.Id).ToList();
        var handler = new UnlockAppEpisodeCommandHandler(db, User(Viewer), TimeProvider.System, Options);

        var unlocked = await handler.Handle(new UnlockAppEpisodeCommand(ids[2]), CancellationToken.None);
        unlocked.Playback.ShouldBe(new AppPlaybackDto("tiktok", "tt3"));
        // Gọi lại không sinh bản trùng.
        await handler.Handle(new UnlockAppEpisodeCommand(ids[2]), CancellationToken.None);
        db.AppEpisodeUnlocks.Count().ShouldBe(1);

        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            handler.Handle(new UnlockAppEpisodeCommand(ids[3]), CancellationToken.None));

        (await EpisodesAsync(db, movie.Id, Viewer)).Select(x => x.Access)
            .ShouldBe([AppAccess.Free, AppAccess.Free, AppAccess.Free, AppAccess.Vip]);
        (await EpisodesAsync(db, movie.Id, "viewer-2"))[2].Access.ShouldBe(AppAccess.Ads);
    }

    [Test]
    public async Task CustomAccess_SetsAdAndVipRangesPerMovie_AndDefaultRestoresSystemRule()
    {
        var (db, movie, _) = await SeedAsync();
        using var __ = db;
        var set = new SetMovieAccessCommandHandler(db, User(Admin));
        async Task<List<string>> Access(string? viewer = null) => (await EpisodesAsync(db, movie.Id, viewer)).Select(x => x.Access).ToList();

        // Tập 2–3 quảng cáo, tập 3 trở đi VIP: tập nằm trong cả hai khoảng là VIP.
        await set.Handle(new SetMovieAccessCommand { MovieId = movie.Id, AdFromEpisode = 2, AdToEpisode = 3, VipFromEpisode = 3 }, CancellationToken.None);
        (await Access()).ShouldBe([AppAccess.Free, AppAccess.Ads, AppAccess.Vip, AppAccess.Vip]);
        var dto = await new GetAppMovieByIdQueryHandler(db, Options).Handle(new GetAppMovieByIdQuery(movie.Id), CancellationToken.None);
        (dto.AdFromEpisode, dto.AdToEpisode, dto.VipFromEpisode, dto.VipToEpisode).ShouldBe((2, 3, 3, null));

        // Mở tập quảng cáo theo cấu hình riêng; tập VIP vẫn bị từ chối.
        var ids = movie.Episodes!.OrderBy(x => x.EpisodeNumber).Select(x => x.Id).ToList();
        var unlock = new UnlockAppEpisodeCommandHandler(db, User(Viewer), TimeProvider.System, Options);
        (await unlock.Handle(new UnlockAppEpisodeCommand(ids[1]), CancellationToken.None)).Access.ShouldBe(AppAccess.Free);
        await Should.ThrowAsync<ForbiddenAccessException>(() => unlock.Handle(new UnlockAppEpisodeCommand(ids[2]), CancellationToken.None));

        // Chỉ có VIP ở giữa phim, không có quảng cáo: tập sau khoảng lại miễn phí.
        await set.Handle(new SetMovieAccessCommand { MovieId = movie.Id, VipFromEpisode = 2, VipToEpisode = 3 }, CancellationToken.None);
        (await Access()).ShouldBe([AppAccess.Free, AppAccess.Vip, AppAccess.Vip, AppAccess.Free]);

        // Cấu hình riêng nhưng để trống hết: cả phim miễn phí.
        await set.Handle(new SetMovieAccessCommand { MovieId = movie.Id }, CancellationToken.None);
        (await Access()).ShouldAllBe(x => x == AppAccess.Free);

        await set.Handle(new SetMovieAccessCommand { MovieId = movie.Id, UseDefault = true, VipFromEpisode = 1 }, CancellationToken.None);
        (await Access()).ShouldBe([AppAccess.Free, AppAccess.Free, AppAccess.Ads, AppAccess.Vip]);
        db.Works.Single(x => x.Id == movie.Id).VipFromEpisode.ShouldBeNull();

        await Should.ThrowAsync<Ardalis.GuardClauses.NotFoundException>(() =>
            new SetMovieAccessCommandHandler(db, User("người-khác")).Handle(new SetMovieAccessCommand { MovieId = movie.Id }, CancellationToken.None));
    }

    [Test]
    public void AccessValidator_RejectsBrokenRanges()
    {
        var validator = new SetMovieAccessCommandValidator();
        validator.Validate(new SetMovieAccessCommand { AdFromEpisode = 4, AdToEpisode = 6, VipFromEpisode = 7 }).IsValid.ShouldBeTrue();
        validator.Validate(new SetMovieAccessCommand { AdFromEpisode = 6, AdToEpisode = 4 }).IsValid.ShouldBeFalse();
        validator.Validate(new SetMovieAccessCommand { VipToEpisode = 9 }).IsValid.ShouldBeFalse();
        validator.Validate(new SetMovieAccessCommand { VipFromEpisode = 0 }).IsValid.ShouldBeFalse();
        // Dùng mặc định thì bỏ qua các ô nhập.
        validator.Validate(new SetMovieAccessCommand { UseDefault = true, AdFromEpisode = 6, AdToEpisode = 4 }).IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task Vip_SeesEveryEpisode_UntilSubscriptionExpires()
    {
        var (db, movie, _) = await SeedAsync();
        using var __ = db;
        var vip = new SetAppViewerVipCommandHandler(db);

        await vip.Handle(new SetAppViewerVipCommand(Viewer, "month", DateTimeOffset.UtcNow.AddDays(30)), CancellationToken.None);
        (await EpisodesAsync(db, movie.Id, Viewer)).ShouldAllBe(x => x.Access == AppAccess.Free && x.Playback != null);

        await vip.Handle(new SetAppViewerVipCommand(Viewer, "month", DateTimeOffset.UtcNow.AddMinutes(-1)), CancellationToken.None);
        db.AppSubscriptions.Count().ShouldBe(1);
        (await EpisodesAsync(db, movie.Id, Viewer))[3].Access.ShouldBe(AppAccess.Vip);
    }

    [Test]
    public async Task Library_BookmarkIsIdempotent_HistoryKeepsNewestProgress_DeleteRemovesEverything()
    {
        var (db, movie, hidden) = await SeedAsync();
        using var __ = db;
        var user = User(Viewer);
        var bookmark = new SetAppBookmarkCommandHandler(db, user);

        await bookmark.Handle(new SetAppBookmarkCommand(movie.Id, true), CancellationToken.None);
        await bookmark.Handle(new SetAppBookmarkCommand(movie.Id, true), CancellationToken.None);
        await Should.ThrowAsync<Ardalis.GuardClauses.NotFoundException>(() =>
            bookmark.Handle(new SetAppBookmarkCommand(hidden.Id, true), CancellationToken.None));

        var saved = await new GetMyBookmarksQueryHandler(db, user, Options).Handle(new GetMyBookmarksQuery(), CancellationToken.None);
        saved.Select(x => x.Id).ShouldBe([movie.Id]);
        saved[0].SaveCount.ShouldBe(1);
        (await new GetMyBookmarksQueryHandler(db, User("viewer-2"), Options).Handle(new GetMyBookmarksQuery(), CancellationToken.None))
            .ShouldBeEmpty();

        var progress = new SaveAppWatchProgressCommandHandler(db, user, TimeProvider.System);
        var now = DateTimeOffset.UtcNow;
        await progress.Handle(new SaveAppWatchProgressCommand
            { MovieId = movie.Id, EpisodeNumber = 3, PositionSeconds = 40, DurationSeconds = 90, WatchedAt = now.AddMinutes(-5) },
            CancellationToken.None);
        // Bản ghi cũ hơn (thiết bị khác đồng bộ muộn) không được ghi đè tiến độ mới.
        await progress.Handle(new SaveAppWatchProgressCommand
            { MovieId = movie.Id, EpisodeNumber = 1, PositionSeconds = 5, DurationSeconds = 90, WatchedAt = now.AddHours(-2) },
            CancellationToken.None);

        var history = await new GetMyHistoryQueryHandler(db, user, Options).Handle(new GetMyHistoryQuery(), CancellationToken.None);
        history.Count.ShouldBe(1);
        history[0].EpisodeNumber.ShouldBe(3);
        history[0].PositionSeconds.ShouldBe(40);

        await new AddAppCommentCommandHandler(db, user)
            .Handle(new AddAppCommentCommand(movie.Id, "  Hay quá  ", "Minh Anh"), CancellationToken.None);
        var comments = await new GetAppCommentsQueryHandler(db, User(null))
            .Handle(new GetAppCommentsQuery(movie.Id), CancellationToken.None);
        comments.Items.Single().Content.ShouldBe("Hay quá");
        comments.Items.Single().IsMine.ShouldBeFalse();

        await new DeleteAppViewerDataCommandHandler(db).Handle(new DeleteAppViewerDataCommand(Viewer), CancellationToken.None);
        db.AppBookmarks.ShouldBeEmpty();
        db.AppWatchHistories.ShouldBeEmpty();
        db.AppComments.ShouldBeEmpty();
    }

    [Test]
    public async Task LibraryCommands_RequireLoggedInViewer()
    {
        var (db, movie, _) = await SeedAsync();
        using var __ = db;

        await Should.ThrowAsync<UnauthorizedAccessException>(() =>
            new SetAppBookmarkCommandHandler(db, User(null)).Handle(new SetAppBookmarkCommand(movie.Id, true), CancellationToken.None));
    }
}
