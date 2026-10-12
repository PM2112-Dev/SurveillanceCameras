using FluentValidation;
using Moq;
using NUnit.Framework;
using Shouldly;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Languages;
using SurveillanceCameras.Application.Local.Languages.Commands;
using SurveillanceCameras.Application.Local.Languages.Queries;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.UnitTests.Languages;

public class LanguageTests
{
    private const string Owner = "user-1";

    private static IUser User()
    {
        var user = new Mock<IUser>();
        user.Setup(x => x.Id).Returns(Owner);
        return user.Object;
    }

    [Test]
    public void Normalize_LowercasesAndRejectsUnknownMarker()
    {
        LanguageCodes.Normalize(" EN ").ShouldBe("en");
        LanguageCodes.Normalize("zh-Hans").ShouldBe("zh-hans");
        LanguageCodes.Normalize("un").ShouldBeNull();
        LanguageCodes.Normalize("").ShouldBeNull();
        LanguageCodes.Normalize(null).ShouldBeNull();
        LanguageCodes.DisplayName("vi").ShouldBe("Tiếng Việt");
        LanguageCodes.DisplayName("xx").ShouldBe("xx");
    }

    [Test]
    public async Task Validator_RejectsDuplicateCodeOfSameOwner_ButAllowsKeepingOwnCode()
    {
        using var db = TestDb.Create();
        var english = new Language { Title = "Tiếng Anh", Code = "en", CreatedBy = Owner };
        db.Languages.AddRange(english, new Language { Title = "Của người khác", Code = "vi", CreatedBy = "user-khác" });
        await db.SaveChangesAsync();
        var create = new CreateLanguageCommandValidator(db, User());

        (await create.ValidateAsync(new CreateLanguageCommand { Title = "English", Code = "EN" })).IsValid.ShouldBeFalse();
        (await create.ValidateAsync(new CreateLanguageCommand { Title = "Tiếng Việt", Code = "vi" })).IsValid.ShouldBeTrue();
        (await create.ValidateAsync(new CreateLanguageCommand { Title = "Không rõ", Code = "un" })).IsValid.ShouldBeFalse();
        (await create.ValidateAsync(new CreateLanguageCommand { Title = "Sai", Code = "e n" })).IsValid.ShouldBeFalse();
        (await create.ValidateAsync(new CreateLanguageCommand { Title = "", Code = "ja" })).IsValid.ShouldBeFalse();

        var update = new UpdateLanguageCommandValidator(db, User());
        (await update.ValidateAsync(new UpdateLanguageCommand { Id = english.Id, Title = "English", Code = "en" })).IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task List_CountsWorksAndEpisodes_AndDeleteUnlinksThem()
    {
        using var db = TestDb.Create();
        var english = new Language { Title = "Tiếng Anh", Code = "en", CreatedBy = Owner };
        var movie = new Work
        {
            Title = "Phim", Type = WorkType.Movie, Language = english, CreatedBy = Owner,
            Episodes = [new Episode { EpisodeNumber = 1, Language = english }, new Episode { EpisodeNumber = 2, Language = english }]
        };
        db.Works.Add(movie);
        db.Languages.Add(new Language { Title = "Của người khác", Code = "en", CreatedBy = "user-khác" });
        await db.SaveChangesAsync();

        var list = await new GetLanguagesQueryHandler(db, User()).Handle(new GetLanguagesQuery { Title = "ANH" }, CancellationToken.None);
        var dto = list.Items.Single();
        dto.Code.ShouldBe("en");
        dto.WorkCount.ShouldBe(1);
        dto.EpisodeCount.ShouldBe(2);

        await new DeleteLanguageCommandHandler(db, User()).Handle(new DeleteLanguageCommand(english.Id), CancellationToken.None);

        db.Languages.Count(x => x.CreatedBy == Owner).ShouldBe(0);
        db.Works.Single().LanguageId.ShouldBeNull();
        db.Episodes.ShouldAllBe(x => x.LanguageId == null);
    }
}
