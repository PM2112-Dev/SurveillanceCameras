using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly.Timeout;

namespace SurveillanceCameras.Infrastructure.IntegrationTests.Repository.AI;

/// <summary>
/// ServiceDefaults gắn AddStandardResilienceHandler (tổng 30 giây) cho mọi HttpClient. Client gọi AI phải gỡ được lớp
/// này — nếu không, chương dài bị huỷ giữa chừng và POST bị tự thử lại (trả phí AI nhiều lần).
/// Test rút ngắn giới hạn xuống 1 giây và dùng server local trả lời sau 3 giây.
/// </summary>
public class AIHttpClientResilienceTests
{
    private static ServiceProvider BuildServices(bool removeResilience)
    {
        var services = new ServiceCollection();
        services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler(options =>
        {
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(1);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(1);
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(2);
        }));

        var client = services.AddHttpClient("ai", c => c.Timeout = TimeSpan.FromSeconds(30));
#pragma warning disable EXTEXP0001
        if (removeResilience) client.RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001

        return services.BuildServiceProvider();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task SlowResponse_OnlySucceedsWhenResilienceRemoved(bool removeResilience)
    {
        using var server = new SlowServer(TimeSpan.FromSeconds(3));
        await using var services = BuildServices(removeResilience);
        var client = services.GetRequiredService<IHttpClientFactory>().CreateClient("ai");

        var send = () => client.PostAsync(server.Url, new StringContent("{}"));

        if (removeResilience)
        {
            var response = await send();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(server.RequestCount, Is.EqualTo(1), "POST không được tự thử lại");
        }
        else
        {
            Assert.ThrowsAsync<TimeoutRejectedException>(async () => await send());
        }
    }

    private sealed class SlowServer : IDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource _cts = new();
        private int _requests;

        public SlowServer(TimeSpan delay)
        {
            _listener.Start();
            Url = $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/";
            _ = Task.Run(async () =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    var socket = await _listener.AcceptTcpClientAsync(_cts.Token);
                    _ = Task.Run(async () =>
                    {
                        using var connection = socket;
                        var stream = connection.GetStream();
                        // Chỉ cần biết đã có request; không cần đọc hết nội dung.
                        _ = await stream.ReadAsync(new byte[4096], _cts.Token);
                        Interlocked.Increment(ref _requests);
                        await Task.Delay(delay, _cts.Token);
                        var body = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nok");
                        await stream.WriteAsync(body, _cts.Token);
                    });
                }
            });
        }

        public string Url { get; }

        public int RequestCount => _requests;

        public void Dispose()
        {
            _cts.Cancel();
            _listener.Stop();
        }
    }
}
