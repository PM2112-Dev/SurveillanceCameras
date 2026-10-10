using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Shouldly;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Movies.Queries.DTOs;
using SurveillanceCameras.Application.Local.Movies.Queries.GetMovies;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.UnitTests.Movies;

public class GetMoviesSortTests
{
    private const string Owner = "user-1";

    [Test]
    public async Task MostViewed_SortsByViewsDescending_UnknownLast_OnlyMoviesOfUser()
    {
        using var db = TestDb.Create();
        db.Works.AddRange(
            new Work { Title = "Ít", Type = WorkType.Movie, ViewCount = 10, CreatedBy = Owner },
            new Work { Title = "Chưa rõ", Type = WorkType.Movie, ViewCount = null, CreatedBy = Owner },
            new Work { Title = "Nhiều", Type = WorkType.Movie, ViewCount = 805_603, CreatedBy = Owner },
            new Work { Title = "Truyện", Type = WorkType.Story, ViewCount = 9_999_999, CreatedBy = Owner },
            new Work { Title = "Người khác", Type = WorkType.Movie, ViewCount = 5_000_000, CreatedBy = "user-khác" });
        await db.SaveChangesAsync();

        var user = new Mock<IUser>();
        user.Setup(x => x.Id).Returns(Owner);
        var mapper = new MapperConfiguration(c => c.AddMaps(typeof(MovieDto).Assembly), NullLoggerFactory.Instance).CreateMapper();

        var result = await new GetMoviesQueryHandler(db, user.Object, mapper)
            .Handle(new GetMoviesQuery { Sort = MovieSort.MostViewed }, CancellationToken.None);

        result.Items.Select(x => x.Title).ShouldBe(["Nhiều", "Ít", "Chưa rõ"]);
        result.Items.First().ViewCount.ShouldBe(805_603);
    }
}
