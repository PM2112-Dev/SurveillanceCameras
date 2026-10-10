using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Shouldly;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Local.AIs;
using SurveillanceCameras.Application.Local.Works.Commands.CreateWorkFromStorySource;
using SurveillanceCameras.Application.Repository.AI;
using SurveillanceCameras.Domain.Constants;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;
using SurveillanceCameras.Infrastructure.Data;

namespace SurveillanceCameras.Application.UnitTests.Works;

public class CreateWorkFromStorySourceTests
{
    private const string Owner = "user-1";

    private ApplicationDbContext _db = null!;
    private Mock<IAITextRepository> _ai = null!;

    [SetUp]
    public void SetUp()
    {
        _db = TestDb.Create();
        _ai = new Mock<IAITextRepository>();
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private CreateWorkFromStorySourceCommandHandler Handler()
        => new(_db, new AIPromptRunner(_db, _ai.Object, NullLogger<AIPromptRunner>.Instance),
            NullLogger<CreateWorkFromStorySourceCommandHandler>.Instance);

    private async Task<StorySource> SeedSourceAsync(string? owner = Owner)
    {
        var source = new StorySource
        {
            Title = "Kiều Loan", Author = "Xán Diêu", Description = "Mô tả gốc", Status = "Hoàn thành",
            WebSourceId = 1, CreatedBy = owner
        };
        _db.StorySources.Add(source);
        await _db.SaveChangesAsync();
        return source;
    }

    private void SeedPrompt(string code, string content, string? owner = Owner)
    {
        var type = new PromptType { Title = code, Code = code, CreatedBy = owner };
        _db.PromptTypes.Add(type);
        _db.Prompts.Add(new Prompt { Title = "p-" + code, Content = content, PromptType = type, CreatedBy = owner });
    }

    private AI SeedAI(string model, string? owner = Owner)
    {
        var ai = new AI { Title = model, Provider = AIProvider.Gemini, Model = model, ApiKey = "key-" + model, CreatedBy = owner };
        _db.AIs.Add(ai);
        return ai;
    }

    [Test]
    public async Task EditsTitleAndDescription_AndCreatesStoryForOwner()
    {
        var source = await SeedSourceAsync();
        SeedPrompt(PromptTypeCodes.EditTitle, "Viết lại tên truyện {title}");
        SeedPrompt(PromptTypeCodes.EditDescription, "Viết lại giới thiệu");
        SeedAI("gemini-2.5-flash");
        await _db.SaveChangesAsync();

        _ai.Setup(x => x.GenerateAsync(It.Is<AITextRequest>(r => r.Instructions.Contains("tên truyện Kiều Loan")), It.IsAny<CancellationToken>()))
            .ReturnsAsync("\"**Kiều Loan Truyện**\"\nGiải thích thừa");
        _ai.Setup(x => x.GenerateAsync(It.Is<AITextRequest>(r => r.Instructions.Contains("giới thiệu")), It.IsAny<CancellationToken>()))
            .ReturnsAsync("  Mô tả mới  ");

        var storyId = await Handler().Handle(new CreateWorkFromStorySourceCommand(source.Id), CancellationToken.None);

        var story = await _db.Works.SingleAsync();
        story.Id.ShouldBe(storyId!.Value);
        story.Title.ShouldBe("Kiều Loan Truyện");          // bỏ ngoặc kép, ** và dòng giải thích
        story.Description.ShouldBe("Mô tả mới");
        story.CreatedBy.ShouldBe(Owner);                    // chạy nền: chủ = chủ của nguồn truyện
        story.StorySourceId.ShouldBe(source.Id);
        story.AccountId.ShouldBeNull();
        story.IsFull.ShouldBeTrue();

        // Tạo truyện xong thì có yêu cầu đồng bộ chương (trang 1) trong outbox.
        (await _db.OutboxMessages.SingleAsync()).EventType.ShouldBe("StoryChaptersSyncRequestedIntegrationEvent");
    }

    [Test]
    public async Task FirstAIFails_FallsBackToNextAI()
    {
        var source = await SeedSourceAsync();
        SeedPrompt(PromptTypeCodes.EditTitle, "Viết lại tên");
        SeedAI("model-a");
        SeedAI("model-b");
        await _db.SaveChangesAsync();

        _ai.Setup(x => x.GenerateAsync(It.Is<AITextRequest>(r => r.Model == "model-a"), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AIProviderException("429 quota"));
        _ai.Setup(x => x.GenerateAsync(It.Is<AITextRequest>(r => r.Model == "model-b"), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Tên mới");

        await Handler().Handle(new CreateWorkFromStorySourceCommand(source.Id), CancellationToken.None);

        (await _db.Works.SingleAsync()).Title.ShouldBe("Tên mới");
    }

    [Test]
    public async Task AllAIsFail_SavesErrorOnSource_AndThrows_WithoutCreatingStory()
    {
        var source = await SeedSourceAsync();
        SeedPrompt(PromptTypeCodes.EditTitle, "Viết lại tên");
        SeedAI("model-a");
        await _db.SaveChangesAsync();

        _ai.Setup(x => x.GenerateAsync(It.IsAny<AITextRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AIProviderException("key sai"));

        await Should.ThrowAsync<AIProviderException>(() =>
            Handler().Handle(new CreateWorkFromStorySourceCommand(source.Id), CancellationToken.None));

        (await _db.Works.CountAsync()).ShouldBe(0);
        (await _db.StorySources.SingleAsync()).StoryCreationError!.ShouldContain("key sai");
    }

    [Test]
    public async Task NoPrompts_CreatesStoryWithOriginalText_WithoutCallingAI()
    {
        var source = await SeedSourceAsync();

        await Handler().Handle(new CreateWorkFromStorySourceCommand(source.Id), CancellationToken.None);

        var story = await _db.Works.SingleAsync();
        story.Title.ShouldBe("Kiều Loan");
        story.Description.ShouldBe("Mô tả gốc");
        _ai.VerifyNoOtherCalls();
    }

    [Test]
    public async Task SourceWithoutEdit_KeepsOriginalText_EvenWithPrompts()
    {
        var webSource = new WebSource { Title = "Wikidich", BaseUrl = "https://truyen.test", IsNeedEdit = false, CreatedBy = Owner };
        _db.WebSources.Add(webSource);
        await _db.SaveChangesAsync();
        var source = await SeedSourceAsync();
        source.WebSourceId = webSource.Id;
        SeedPrompt(PromptTypeCodes.EditTitle, "Viết lại tên");
        SeedPrompt(PromptTypeCodes.EditDescription, "Viết lại giới thiệu");
        SeedAI("gemini-2.5-flash");
        await _db.SaveChangesAsync();

        await Handler().Handle(new CreateWorkFromStorySourceCommand(source.Id), CancellationToken.None);

        var story = await _db.Works.SingleAsync();
        story.Title.ShouldBe("Kiều Loan");
        story.Description.ShouldBe("Mô tả gốc");
        _ai.VerifyNoOtherCalls();
        _db.OutboxEventTypes().ShouldContain("StoryChaptersSyncRequestedIntegrationEvent");
    }

    [Test]
    public async Task SourceWithTytAccount_AssignsAccount_AndQueuesTytCreation()
    {
        var webSource = new WebSource { Title = "Nguồn", BaseUrl = "https://truyen.test", TytAccountId = 5, CreatedBy = Owner };
        _db.WebSources.Add(webSource);
        await _db.SaveChangesAsync();
        var source = await SeedSourceAsync();
        source.WebSourceId = webSource.Id;
        await _db.SaveChangesAsync();

        await Handler().Handle(new CreateWorkFromStorySourceCommand(source.Id), CancellationToken.None);

        (await _db.Works.SingleAsync()).AccountId.ShouldBe(5);
        _db.OutboxEventTypes().ShouldContain("TytStoryCreateRequestedIntegrationEvent");
    }

    [Test]
    public async Task SourceWithoutTytAccount_DoesNotQueueTytCreation()
    {
        var source = await SeedSourceAsync();

        await Handler().Handle(new CreateWorkFromStorySourceCommand(source.Id), CancellationToken.None);

        _db.OutboxEventTypes().ShouldNotContain("TytStoryCreateRequestedIntegrationEvent");
    }

    [Test]
    public async Task IgnoresPromptsAndAIsOfOtherUsers()
    {
        var source = await SeedSourceAsync();
        SeedPrompt(PromptTypeCodes.EditTitle, "Prompt của người khác", owner: "user-2");
        SeedAI("model-of-user-2", owner: "user-2");
        await _db.SaveChangesAsync();

        await Handler().Handle(new CreateWorkFromStorySourceCommand(source.Id), CancellationToken.None);

        (await _db.Works.SingleAsync()).Title.ShouldBe("Kiều Loan");
        _ai.VerifyNoOtherCalls();
    }

    [Test]
    public async Task CalledTwice_DoesNotCreateDuplicateStory()
    {
        var source = await SeedSourceAsync();

        var first = await Handler().Handle(new CreateWorkFromStorySourceCommand(source.Id), CancellationToken.None);
        var second = await Handler().Handle(new CreateWorkFromStorySourceCommand(source.Id), CancellationToken.None);

        second.ShouldBe(first);
        (await _db.Works.CountAsync()).ShouldBe(1);
    }

    [Test]
    public async Task PromptExistsButNoAI_Throws()
    {
        var source = await SeedSourceAsync();
        SeedPrompt(PromptTypeCodes.EditTitle, "Viết lại tên");
        await _db.SaveChangesAsync();

        var ex = await Should.ThrowAsync<AIProviderException>(() =>
            Handler().Handle(new CreateWorkFromStorySourceCommand(source.Id), CancellationToken.None));

        ex.Message.ShouldContain("Chưa khai báo AI");
    }
}
