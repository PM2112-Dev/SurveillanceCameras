using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Shouldly;
using Ardalis.GuardClauses;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Episodes.Queries.GetEpisodes;
using SurveillanceCameras.Application.Local.Works.Commands.DeleteWork;
using SurveillanceCameras.Application.Local.Works.Queries.DTOs;
using SurveillanceCameras.Application.Local.Works.Queries.GetWorkById;
using SurveillanceCameras.Application.Local.Works.Queries.GetWorks;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.UnitTests.Works;

/// <summary>Truyện và phim cùng nằm trong bảng Works; phía Truyện không được thấy hay sửa phim.</summary>
public class WorkTypeSeparationTests
{
    private const string Owner = "user-1";

    private static IUser User()
    {
        var user = new Mock<IUser>();
        user.Setup(x => x.Id).Returns(Owner);
        return user.Object;
    }

    private static IMapper Mapper()
        => new MapperConfiguration(c => c.AddMaps(typeof(WorkDto).Assembly), NullLoggerFactory.Instance).CreateMapper();

    private static async Task<(Infrastructure.Data.ApplicationDbContext Db, Work Story, Work Movie)> SeedAsync()
    {
        var db = TestDb.Create();
        var story = new Work { Title = "Truyện chữ", Type = WorkType.Story, CreatedBy = Owner, Episodes = [] };
        var movie = new Work { Title = "Phim TikTok", Type = WorkType.Movie, CreatedBy = Owner, Episodes = [] };
        story.Episodes.Add(new Episode { EpisodeNumber = 1, NameRaw = "Chương 1", CreatedBy = Owner });
        movie.Episodes.Add(new Episode { EpisodeNumber = 1, NameRaw = "Tập 1", CreatedBy = Owner });
        db.Works.AddRange(story, movie);
        await db.SaveChangesAsync();
        return (db, story, movie);
    }

    [Test]
    public async Task StoryList_AndById_DoNotIncludeMovies()
    {
        var (db, story, movie) = await SeedAsync();
        using var _ = db;

        var list = await new GetWorksQueryHandler(db, User(), Mapper()).Handle(new GetWorksQuery(), CancellationToken.None);
        list.Items.Select(x => x.Title).ShouldBe(["Truyện chữ"]);
        list.TotalCount.ShouldBe(1);

        (await new GetWorkByIdQueryHandler(db, User(), Mapper()).Handle(new GetWorkByIdQuery(story.Id), CancellationToken.None))
            .Title.ShouldBe("Truyện chữ");
        await Should.ThrowAsync<NotFoundException>(() =>
            new GetWorkByIdQueryHandler(db, User(), Mapper()).Handle(new GetWorkByIdQuery(movie.Id), CancellationToken.None));
    }

    [Test]
    public async Task StoryEpisodes_AndDelete_RefuseMovies()
    {
        var (db, story, movie) = await SeedAsync();
        using var _ = db;

        var chapters = await new GetEpisodesQueryHandler(db, Mapper(), User())
            .Handle(new GetEpisodesQuery { WorkId = story.Id }, CancellationToken.None);
        chapters.TotalCount.ShouldBe(1);

        var ofMovie = await new GetEpisodesQueryHandler(db, Mapper(), User())
            .Handle(new GetEpisodesQuery { WorkId = movie.Id }, CancellationToken.None);
        ofMovie.TotalCount.ShouldBe(0);

        await Should.ThrowAsync<NotFoundException>(() =>
            new DeleteWorkCommandHandler(db, User()).Handle(new DeleteWorkCommand(movie.Id), CancellationToken.None));
        db.Works.Count().ShouldBe(2);
    }
}
