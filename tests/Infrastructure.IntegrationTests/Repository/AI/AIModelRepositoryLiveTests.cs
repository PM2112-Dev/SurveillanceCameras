using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Domain.Enums;
using SurveillanceCameras.Infrastructure.Repository.AI;

namespace SurveillanceCameras.Infrastructure.IntegrationTests.Repository.AI;

/// <summary>
/// Gọi thật API của từng nền tảng với key giả: kiểm tra đúng endpoint/header và key sai được báo lỗi rõ ràng
/// (không tốn phí). Chạy khi chỉ định: <c>dotnet test --filter Category=LiveAI</c>.
/// </summary>
[TestFixture, Explicit, Category("LiveAI")]
public class AIModelRepositoryLiveTests
{
    [TestCase(AIProvider.Gemini)]
    [TestCase(AIProvider.Claude)]
    [TestCase(AIProvider.ChatGpt)]
    [TestCase(AIProvider.DeepSeek)]
    [TestCase(AIProvider.Grok)]
    public void InvalidKey_ThrowsReadableError(AIProvider provider)
    {
        var repository = new AIModelRepository(new PlainClientFactory());

        var ex = Assert.ThrowsAsync<AIProviderException>(() => repository.ListModelsAsync(provider, "invalid-test-key-123456"));

        TestContext.Out.WriteLine($"{provider}: {ex!.Message}");
    }

    [TestCase(AIProvider.Gemini, "gemini-2.5-flash")]
    [TestCase(AIProvider.Claude, "claude-opus-5")]
    [TestCase(AIProvider.ChatGpt, "gpt-5-mini")]
    [TestCase(AIProvider.DeepSeek, "deepseek-chat")]
    [TestCase(AIProvider.Grok, "grok-3-mini")]
    public void Generate_InvalidKey_ThrowsReadableError(AIProvider provider, string model)
    {
        var repository = new AITextRepository(new PlainClientFactory());
        var request = new Application.Repository.AI.AITextRequest(
            provider, model, "invalid-test-key-123456", "Chỉ trả về tên truyện.", "Tên truyện: Thử nghiệm");

        var ex = Assert.ThrowsAsync<AIProviderException>(() => repository.GenerateAsync(request));

        TestContext.Out.WriteLine($"{provider}: {ex!.Message}");
    }

    private sealed class PlainClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new() { Timeout = TimeSpan.FromSeconds(20) };
    }
}
