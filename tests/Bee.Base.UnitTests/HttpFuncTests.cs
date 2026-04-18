using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Bee.Base.UnitTests
{
    public class HttpFuncTests
    {
        [Theory]
        [InlineData("http://example.com", true)]
        [InlineData("https://example.com/path?x=1", true)]
        [InlineData("HTTP://example.com", true)]
        [InlineData("ftp://example.com", false)]
        [InlineData("not-a-url", false)]
        [InlineData("", false)]
        [InlineData("/local/path", false)]
        [DisplayName("IsUrl 應僅對絕對 http/https URL 回傳 true")]
        public void IsUrl_RecognizesHttpSchemes(string input, bool expected)
        {
            Assert.Equal(expected, HttpFunc.IsUrl(input));
        }

        [Fact]
        [DisplayName("GetAsync 應將 Header 傳遞並取回回應主體")]
        public async Task GetAsync_SendsRequestAndReturnsBody()
        {
            await using var server = await LoopbackHttpServer.StartAsync();

            var headers = new NameValueCollection { { "X-Test", "abc" } };
            string result = await HttpFunc.GetAsync(server.BuildUrl("/ping"), headers);

            Assert.Equal("pong", result);
            Assert.Contains("GET /ping", server.LastRequest);
            Assert.Contains("X-Test: abc", server.LastRequest);
        }

        [Fact]
        [DisplayName("PostAsync 應以 JSON Content-Type 送出 body 並取回回應")]
        public async Task PostAsync_SendsJsonBodyAndReturnsResponse()
        {
            await using var server = await LoopbackHttpServer.StartAsync();

            var result = await HttpFunc.PostAsync(server.BuildUrl("/submit"), "{\"k\":1}");

            Assert.Equal("pong", result);
            Assert.Contains("POST /submit", server.LastRequest);
            Assert.Contains("Content-Type: application/json", server.LastRequest);
            Assert.Contains("{\"k\":1}", server.LastRequest);
        }

        [Fact]
        [DisplayName("GetAsync 於 HTTP 非 2xx 回應應拋出 HttpRequestException")]
        public async Task GetAsync_NonSuccessStatus_Throws()
        {
            await using var server = await LoopbackHttpServer.StartAsync(statusLine: "HTTP/1.1 500 Internal Server Error", body: "fail");

            await Assert.ThrowsAsync<HttpRequestException>(
                () => HttpFunc.GetAsync(server.BuildUrl("/boom")));
        }

        /// <summary>
        /// Minimal single-request HTTP loopback server used to exercise HttpFunc without requiring
        /// external infrastructure or mocking HttpClient internals.
        /// </summary>
        private sealed class LoopbackHttpServer : IAsyncDisposable
        {
            private readonly TcpListener _listener;
            private readonly CancellationTokenSource _cts = new();
            private readonly Task _acceptLoop;
            private readonly string _statusLine;
            private readonly string _body;
            private string _lastRequest = string.Empty;

            public int Port { get; }
            public string LastRequest => Volatile.Read(ref _lastRequest);

            private LoopbackHttpServer(TcpListener listener, int port, string statusLine, string body)
            {
                _listener = listener;
                _statusLine = statusLine;
                _body = body;
                Port = port;

                _acceptLoop = Task.Run(RunAcceptLoopAsync);
            }

            private async Task RunAcceptLoopAsync()
            {
                try
                {
                    while (!_cts.IsCancellationRequested)
                    {
                        TcpClient client;
                        try
                        {
                            client = await _listener.AcceptTcpClientAsync(_cts.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }

                        await HandleClientAsync(client);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Listener was stopped concurrently.
                }
                catch (SocketException)
                {
                    // Listener was stopped while accepting.
                }
            }

            private async Task HandleClientAsync(TcpClient client)
            {
                using (client)
                using (var ns = client.GetStream())
                {
                    var buffer = new byte[4096];
                    var sb = new StringBuilder();
                    int headerEnd = -1;
                    int contentLength = 0;

                    while (headerEnd < 0)
                    {
                        int read = await ns.ReadAsync(buffer);
                        if (read == 0) break;
                        sb.Append(Encoding.UTF8.GetString(buffer, 0, read));
                        int idx = sb.ToString().IndexOf("\r\n\r\n", StringComparison.Ordinal);
                        if (idx >= 0)
                        {
                            headerEnd = idx + 4;
                            foreach (var line in sb.ToString(0, idx).Split("\r\n"))
                            {
                                if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                                {
                                    _ = int.TryParse(line.AsSpan("Content-Length:".Length).Trim(), out contentLength);
                                    break;
                                }
                            }
                        }
                    }

                    int bodyRead = Math.Max(0, sb.Length - Math.Max(headerEnd, 0));
                    while (headerEnd >= 0 && bodyRead < contentLength)
                    {
                        int read = await ns.ReadAsync(buffer);
                        if (read == 0) break;
                        sb.Append(Encoding.UTF8.GetString(buffer, 0, read));
                        bodyRead += read;
                    }

                    Volatile.Write(ref _lastRequest, sb.ToString());

                    byte[] bodyBytes = Encoding.UTF8.GetBytes(_body);
                    string response =
                        $"{_statusLine}\r\n" +
                        "Content-Type: text/plain; charset=utf-8\r\n" +
                        $"Content-Length: {bodyBytes.Length}\r\n" +
                        "Connection: close\r\n\r\n";
                    byte[] header = Encoding.UTF8.GetBytes(response);
                    await ns.WriteAsync(header);
                    await ns.WriteAsync(bodyBytes);
                    await ns.FlushAsync();
                }
            }

            public static Task<LoopbackHttpServer> StartAsync(
                string statusLine = "HTTP/1.1 200 OK",
                string body = "pong")
            {
                var listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                int port = ((IPEndPoint)listener.LocalEndpoint).Port;
                return Task.FromResult(new LoopbackHttpServer(listener, port, statusLine, body));
            }

            public string BuildUrl(string path) => $"http://127.0.0.1:{Port}{path}";

            public async ValueTask DisposeAsync()
            {
                _cts.Cancel();
                _listener.Stop();
                try
                {
                    await _acceptLoop;
                }
                catch (OperationCanceledException)
                {
                    // Expected during cancellation.
                }
                catch (IOException)
                {
                    // Stream torn down during shutdown.
                }
                _cts.Dispose();
            }
        }
    }
}
