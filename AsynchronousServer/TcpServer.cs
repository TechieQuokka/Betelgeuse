using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using AsynchronousServer.DataType;
using AsynchronousServer.StaticMethod;
using Standard.Static;

namespace AsynchronousServer
{
    public class TcpServer : IServer
    {
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ConcurrentDictionary<Guid, ConnectedClient> _connectedClients;
        private readonly int _chunkSize;
        private readonly IPAddress _ipAddress;
        private readonly int _port;
        private byte[] _buffer;

        int IServer.ConnectedClientCount => this._connectedClients.Count;
        public int ChunkSize => this._chunkSize;

        private bool disposedValue;

        public event EventHandler? Enter;
        public event StartServerEventHandler? Connected;
        public event EventHandler<ConnectedClient>? EnterClientCommunication;
        public event ClientCommunicationEventHandler? ReceiveData;
        public event EventHandler? StopServer;

        public TcpServer (IPAddress ipAddress, int port, int chunkSize = 65536)
        {
            this._cancellationTokenSource = new CancellationTokenSource();
            this._connectedClients = new ConcurrentDictionary<Guid, ConnectedClient>();
            this._chunkSize = chunkSize;
            this._ipAddress = ipAddress;
            this._port = port;
            this._buffer = new byte[chunkSize];
            return;
        }

        public async Task StartServerAsync()
        {
            using (var server = new TcpListener(this._ipAddress, this._port))
            {
                this.Enter?.Invoke(this, new EventArgs());
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

        public void SendInChunks(Stream stream, byte[] data)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentNullException.ThrowIfNull(data);

            // Sending data size
            var dataSizeBuffer = BitConverter.GetBytes(data.Length);
            stream.Write(dataSizeBuffer, 0, dataSizeBuffer.Length);

            for (int index = 0; index < data.Length; index += this._chunkSize)
            {
                int currentChunkSize = Math.Min(this._chunkSize, data.Length - index);
                stream.Write(data, index, currentChunkSize);
            }

            stream.Flush();
            return;
        }

        public byte[] ReceiveInChunks(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            // 데이터 크기 읽기
            var dataSizeBuffer = new byte[sizeof(int)];
            _ = stream.Read(dataSizeBuffer, 0, dataSizeBuffer.Length);
            int dataSize = BitConverter.ToInt32(dataSizeBuffer, 0);

            var receivedData = new List<byte>();
            var buffer = this._buffer;
            int bytesRead;
            int totalBytesRead = 0;

            while (totalBytesRead < dataSize && (bytesRead = stream.Read(buffer, 0, this._chunkSize)) > 0)
            {
                receivedData.AddRange(buffer.Take(bytesRead));
                totalBytesRead += bytesRead;
            }

            return receivedData.ToArray();
        }

        private void DisconnectAllEvents()
        {
            this.Enter.UnsubscribeAllHandlers<EventHandler>((handler) => this.Enter -= handler);
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
