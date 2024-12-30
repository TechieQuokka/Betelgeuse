using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using AsynchronousServer.DataType;
using AsynchronousServer.StaticMethod;
using Standard.Static;

namespace AsynchronousServer
{
    public class TcpServer : IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ConcurrentDictionary<Guid, ConnectedClient> _connectedClients;
        private readonly int _chunkSize;

        public int Count => this._connectedClients.Count;

        private bool disposedValue;

        public event EventHandler<TcpListener>? Enter;
        public event StartServerEventHandler? Connected;
        public event EventHandler<ConnectedClient>? EnterClientCommunication;
        public event ClientCommunicationEventHandler? ReceiveData;
        public event EventHandler? StopServer;

        public TcpServer (int chunkSize = 65536)
        {
            this._cancellationTokenSource = new CancellationTokenSource();
            this._connectedClients = new ConcurrentDictionary<Guid, ConnectedClient>();
            this._chunkSize = chunkSize;
            return;
        }

        public async Task StartServerAsync(IPAddress ipAddress, int port)
        {
            using (var server = new TcpListener(ipAddress, port))
            {
                this.Enter?.Invoke(this, server);
                server.Start();

                while (!this._cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var client = await server.AcceptTcpClientAsync();

                    var clientId = Guid.NewGuid();
                    var connectedClient = new ConnectedClient(clientId, client.GetStream());
                    this._connectedClients[clientId] = connectedClient;
                    this.Connected?.Invoke(this, new StartServerEventArgs(connectedClient.MyStream, this._connectedClients, clientId, string.Empty));

                    _ = HandleClientCommunicationAsync(client, connectedClient, this._connectedClients, this._cancellationTokenSource, this._chunkSize).ContinueWith(task =>
                    {
                        if (task.Exception != null)
                        {
                            Console.WriteLine($"Client communication error: {task.Exception.InnerException?.Message}");
                        }
                    }, TaskContinuationOptions.OnlyOnRanToCompletion);
                }

                server.Stop();
            }
        }

        private async Task HandleClientCommunicationAsync (TcpClient client, ConnectedClient connectedClient, ConcurrentDictionary<Guid, ConnectedClient> connectedClients, CancellationTokenSource cancellationTokenSource, int chunkSize)
        {
            try
            {
                this.EnterClientCommunication?.Invoke(this, connectedClient);

                while (client.Connected)
                {
                    var data = await connectedClient.MyStream.ReceiveInChunksAsync(chunkSize);
                    if (data is null || data.Length == 0) continue;

                    this.ReceiveData?.Invoke(this, new ClientCommunicationEventArgs(connectedClient, data, string.Empty, cancellationTokenSource, connectedClients));
                }
            }
            finally
            {
                connectedClients.TryRemove(connectedClient.Id, out _);
                client.Close();
                client.Dispose();
            }
            return;
        }

        public void Stop()
        {
            this.StopServer?.Invoke(this, new EventArgs());
            this._cancellationTokenSource?.Cancel();
        }

        private void DisconnectAllEvents()
        {
            this.Enter.UnsubscribeAllHandlers<EventHandler<TcpListener>>((handler) => this.Enter -= handler);
            this.Connected.UnsubscribeAllHandlers<StartServerEventHandler>((handler) => this.Connected -= handler);
            this.EnterClientCommunication.UnsubscribeAllHandlers<EventHandler<ConnectedClient>>((handler) => this.EnterClientCommunication -= handler);
            this.ReceiveData.UnsubscribeAllHandlers<ClientCommunicationEventHandler>((handler) => this.ReceiveData -= handler);
            this.StopServer.UnsubscribeAllHandlers<EventHandler>((handler) => this.StopServer -= handler);
            return;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 관리형 상태(관리형 개체)를 삭제합니다.
                    this._cancellationTokenSource?.Cancel();
                    this._cancellationTokenSource?.Dispose();
                }

                // TODO: 비관리형 리소스(비관리형 개체)를 해제하고 종료자를 재정의합니다.
                // TODO: 큰 필드를 null로 설정합니다.
                this.DisconnectAllEvents();
                disposedValue = true;
            }
        }

        // // TODO: 비관리형 리소스를 해제하는 코드가 'Dispose(bool disposing)'에 포함된 경우에만 종료자를 재정의합니다.
        // ~TcpServer()
        // {
        //     // 이 코드를 변경하지 마세요. 'Dispose(bool disposing)' 메서드에 정리 코드를 입력합니다.
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 이 코드를 변경하지 마세요. 'Dispose(bool disposing)' 메서드에 정리 코드를 입력합니다.
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
