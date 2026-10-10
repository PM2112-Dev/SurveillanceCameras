using System.Net;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Infrastructure.Repository.Media;

namespace SurveillanceCameras.Infrastructure.IntegrationTests.Repository.Media;

public class LocalMediaRepositoryTests
{
    private string _root = null!;
    private MockHttpMessageHandler _http = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "media-test-" + Guid.NewGuid().ToString("N"));
        _http = new MockHttpMessageHandler();
    }

    [TearDown]
    public void TearDown()
    {
        _http.Dispose();
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    private LocalMediaRepository Repository(long maxBytes = 1024)
        => new(new SingleClientFactory(_http.ToHttpClient()),
            Options.Create(new MediaOptions { RootDirectory = _root, MaxImageBytes = maxBytes }));

    private void Respond(string url, string contentType, byte[] body)
        => _http.When(url).Respond(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(body) { Headers = { ContentType = new(contentType) } }
        });

    [Test]
    public async Task SavesImage_WithExtensionFromContentType_AndReplacesOldFile()
    {
        Respond("https://cdn.test/a", "image/png", [1, 2, 3]);
        Respond("https://cdn.test/b", "image/jpeg", [4, 5]);
        var repository = Repository();

        Assert.That(await repository.SaveImageFromUrlAsync("https://cdn.test/a", "covers/movies", "12"), Is.EqualTo("/media/covers/movies/12.png"));
        var url = await repository.SaveImageFromUrlAsync("https://cdn.test/b", "covers/movies", "12");

        Assert.That(url, Is.EqualTo("/media/covers/movies/12.jpg"));
        Assert.That(repository.IsStored(url), Is.True);
        Assert.That(repository.IsStored("/media/covers/movies/12.png"), Is.False);
        Assert.That(await File.ReadAllBytesAsync(Path.Combine(_root, "covers/movies/12.jpg")), Is.EqualTo(new byte[] { 4, 5 }));
    }

    [Test]
    public void RejectsNonImage_TooLarge_NonHttps_AndPathTraversal()
    {
        Respond("https://cdn.test/html", "text/html", [1]);
        Respond("https://cdn.test/big", "image/jpeg", new byte[2048]);
        var repository = Repository();

        Assert.ThrowsAsync<MediaException>(() => repository.SaveImageFromUrlAsync("https://cdn.test/html", "covers", "1"));
        Assert.ThrowsAsync<MediaException>(() => repository.SaveImageFromUrlAsync("https://cdn.test/big", "covers", "1"));
        Assert.ThrowsAsync<MediaException>(() => repository.SaveImageFromUrlAsync("http://cdn.test/a", "covers", "1"));
        Assert.ThrowsAsync<MediaException>(() => repository.SaveImageFromUrlAsync("https://cdn.test/a", "../etc", "1"));
        Assert.That(repository.IsStored("/media/../secret.txt"), Is.False);
        Assert.That(Directory.Exists(Path.Combine(_root, "covers")) ? Directory.GetFiles(Path.Combine(_root, "covers")) : [], Is.Empty);
    }

    private sealed class SingleClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }
}
