using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Shouldly;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Local.AIs;
using SurveillanceCameras.Application.Repository.AI;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.UnitTests.AIs;

/// <summary>Key hết quota thì nghỉ và chuyển sang key khác; request sau không gọi lại key đang nghỉ.</summary>
public class AIPromptRunnerFailoverTests
{
    private sealed class Clock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now = now;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static AI Key(int id) => new() { Id = id, Title = $"key{id}", Model = "m", ApiKey = $"k{id}" };

    private readonly Prompt _prompt = new() { Title = "Edit chapter", Content = "x" };

    [Test]
    public async Task ExhaustedKeys_AreSkippedOnLaterRequests_AndNewKeysGetUsed()
    {
        var calls = new List<string>();
        var ai = new Mock<IAITextRepository>();
        ai.Setup(x => x.GenerateAsync(It.IsAny<AITextRequest>(), It.IsAny<CancellationToken>()))
            .Returns<AITextRequest, CancellationToken>((r, _) =>
            {
                calls.Add(r.ApiKey);
                return r.ApiKey == "k5" ? Task.FromResult("xong")
                    : throw new AIKeyUnavailableException("quota 429", TimeSpan.FromHours(8));
            });
        var clock = new Clock(new DateTimeOffset(2026, 10, 22, 0, 0, 0, TimeSpan.Zero));
        var runner = new AIPromptRunner(null!, ai.Object, NullLogger<AIPromptRunner>.Instance, new AIKeyCooldowns(), clock);
        List<AI> keys = [Key(1), Key(2), Key(3), Key(4), Key(5)];

        (await runner.RunAsync(_prompt, keys, "i", "in", CancellationToken.None)).ShouldBe("xong");
        calls.ShouldBe(["k1", "k2", "k3", "k4", "k5"]);

        // Yêu cầu sau: bốn key đầu đang nghỉ nên đi thẳng key 5.
        calls.Clear();
        (await runner.RunAsync(_prompt, keys, "i", "in", CancellationToken.None)).ShouldBe("xong");
        calls.ShouldBe(["k5"]);

        // Sau 8 giờ các key đầu được thử lại.
        calls.Clear();
        clock.Now = clock.Now.AddHours(8).AddMinutes(1);
        await runner.RunAsync(_prompt, keys, "i", "in", CancellationToken.None);
        calls.First().ShouldBe("k1");
    }

    [Test]
    public async Task WhenEveryKeyIsResting_NoRequestIsSent_AndTheMessageSaysWhenItWorksAgain()
    {
        var ai = new Mock<IAITextRepository>();
        ai.Setup(x => x.GenerateAsync(It.IsAny<AITextRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AIKeyUnavailableException("quota 429", TimeSpan.FromHours(2)));
        var runner = new AIPromptRunner(null!, ai.Object, NullLogger<AIPromptRunner>.Instance, new AIKeyCooldowns(),
            new Clock(DateTimeOffset.UtcNow));
        List<AI> keys = [Key(1), Key(2)];

        await Should.ThrowAsync<AIProviderException>(() => runner.RunAsync(_prompt, keys, "i", "in", CancellationToken.None));
        ai.Invocations.Clear();

        var ex = await Should.ThrowAsync<AIProviderException>(() => runner.RunAsync(_prompt, keys, "i", "in", CancellationToken.None));

        ex.Message.ShouldContain("2 key đang nghỉ");
        ai.VerifyNoOtherCalls();
    }

    [Test]
    public async Task OtherErrors_DoNotPutAKeyToRest()
    {
        var calls = 0;
        var ai = new Mock<IAITextRepository>();
        ai.Setup(x => x.GenerateAsync(It.IsAny<AITextRequest>(), It.IsAny<CancellationToken>()))
            .Returns<AITextRequest, CancellationToken>((_, _) => ++calls == 1 ? throw new AIProviderException("nội dung bị chặn") : Task.FromResult("ok"));
        var runner = new AIPromptRunner(null!, ai.Object, NullLogger<AIPromptRunner>.Instance, new AIKeyCooldowns(),
            new Clock(DateTimeOffset.UtcNow));
        List<AI> keys = [Key(1), Key(2)];

        (await runner.RunAsync(_prompt, keys, "i", "in", CancellationToken.None)).ShouldBe("ok");   // key 2 trả lời
        calls = 0;
        (await runner.RunAsync(_prompt, keys, "i", "in", CancellationToken.None)).ShouldBe("ok");   // key 1 vẫn được thử lại
        calls.ShouldBe(2);
    }
}
