using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Playwright.Transport
{
    internal class WebSocketTransport : IConnectionTransport, IDisposable
    {
        private const int DefaultBufferSize = 16000;  // Byte buffer size
        private readonly ClientWebSocket _webSocket;
        private readonly string _wsEndpoint;
        private readonly BrowserTypeConnectOptions _options;
        private readonly float _slowMo;
        private CancellationTokenSource _connectCancellationSource;

        internal WebSocketTransport(
            string wsEndpoint = default,
            BrowserTypeConnectOptions options = default)
        {
            _webSocket = new ClientWebSocket();
            _wsEndpoint = wsEndpoint;
            _options = options;
            _slowMo = _options?.SlowMo ?? 0;
            SetRequestHeaders();
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        ~WebSocketTransport() => Dispose(false);

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public event EventHandler<LogReceivedEventArgs> LogReceived;

        public event EventHandler<TransportClosedEventArgs> TransportClosed;

        public bool IsClosed { get; private set; }

        public async Task SendAsync(string message)
        {
            await Task.Delay((int)_slowMo).ConfigureAwait(false);

            try
            {
                var messageBuffer = Encoding.UTF8.GetBytes(message);
                await _webSocket.SendAsync(new ArraySegment<byte>(messageBuffer, 0, messageBuffer.Length), WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Close(ex);
            }
        }

        /// <inheritdoc/>
        public void Close(string closeReason)
        {
            if (!IsClosed)
            {
                IsClosed = true;
                HandleSocketClosed(closeReason);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task ConnectAsync()
        {
            var timeout = _options?.Timeout ?? 30000;
            _connectCancellationSource = new CancellationTokenSource((int)timeout);
            await _webSocket.ConnectAsync(new Uri(_wsEndpoint), _connectCancellationSource.Token).ConfigureAwait(false);
            ScheduleTransportTask(DispatchIncomingMessagesAsync, CancellationToken.None);
        }

        private void Close(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            Close(ex.ToString());
        }

        private void HandleSocketClosed(string closeReason)
        {
            TransportClosed?.Invoke(this, new TransportClosedEventArgs { CloseReason = closeReason });
        }

        private void ScheduleTransportTask(Func<CancellationToken, Task> func, CancellationToken cancellationToken)
            => Task.Factory.StartNew(() => func(cancellationToken), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);

        private async Task DispatchIncomingMessagesAsync(CancellationToken token)
        {
            try
            {
                if (_webSocket.State == WebSocketState.Closed)
                {
                    HandleSocketClosed("Closed");
                }
                else
                {
                    var buffer = WebSocket.CreateClientBuffer(DefaultBufferSize, DefaultBufferSize);

                    while (!token.IsCancellationRequested && _webSocket.State == WebSocketState.Open)
                    {
                        WebSocketReceiveResult result;
                        using (MemoryStream memoryStream = new())
                        {
                            do
                            {
                                result = await _webSocket.ReceiveAsync(buffer, token).ConfigureAwait(false);

                                if (result.MessageType == WebSocketMessageType.Close)
                                {
                                    HandleSocketClosed("Closed");
                                }
                                else
                                {
#pragma warning disable VSTHRD103 // Call async methods when in an async method
                                    memoryStream.Write(buffer.Array, 0, result.Count);
#pragma warning restore VSTHRD103
                                }
                            }
                            while (!result.EndOfMessage);

                            string output = Encoding.UTF8.GetString(memoryStream.ToArray());
                            MessageReceived?.Invoke(this, new MessageReceivedEventArgs(output));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Close(ex);
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _connectCancellationSource?.Dispose();
            _webSocket?.Dispose();
        }

        private string GenerateUserAgent()
        {
            var architecture = RuntimeInformation.OSArchitecture;
            var osAndVersion = RuntimeInformation.OSDescription;
            var frameworkDescription = RuntimeInformation.FrameworkDescription;
            var assemblyVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
            return $"Playwright/{assemblyVersion} {frameworkDescription} ({architecture}/{osAndVersion})";
        }

        private void SetRequestHeaders()
        {
            _webSocket.Options.SetRequestHeader("User-Agent", GenerateUserAgent());
            foreach (var item in _options?.Headers ?? Array.Empty<KeyValuePair<string, string>>())
            {
                _webSocket.Options.SetRequestHeader(item.Key, item.Value);
            }
        }
    }
}