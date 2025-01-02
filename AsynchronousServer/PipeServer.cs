using System.Collections.Concurrent;
using System.IO.Pipes;
using AsynchronousServer.DataType;
using AsynchronousServer.StaticMethod;
using static Standard.Static.Free;

namespace AsynchronousServer
{
    public delegate void StartServerEventHandler(object sender, StartServerEventArgs argument);
    public delegate void ClientCommunicationEventHandler(object sender, ClientCommunicationEventArgs argument);

    public class PipeServer : IServer
    {
        private readonly string _pipeName;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ConcurrentDictionary<Guid, ConnectedClient> _connectedClients;
        private readonly int _chunkSize;
        private readonly int _maxNumberOfServerInstances;
        private bool disposedValue;
        private byte[] _buffer;

        public event EventHandler? Enter;
        public event StartServerEventHandler? Connected;
        public event EventHandler<ConnectedClient>? EnterClientCommunication;
        public event ClientCommunicationEventHandler? ReceiveData;
        public event EventHandler? StopServer;

        public int ChunkSize => this._chunkSize;

        int IServer.ConnectedClientCount { get => this._connectedClients.Count; }

        public PipeServer (string pipeName, int maxNumberOfServerInstances = 10, int chunkSize = 65536)
        {
            this._pipeName = pipeName;
            this._cancellationTokenSource = new CancellationTokenSource ();
            this._connectedClients = new ConcurrentDictionary<Guid, ConnectedClient>();
            this._chunkSize = chunkSize;
            this._maxNumberOfServerInstances = maxNumberOfServerInstances;
            this._buffer = new byte[chunkSize];
            return;
        }

        public async Task StartServerAsync()
        {
            this.Enter?.Invoke(this, new EventArgs());

            while (!this._cancellationTokenSource.Token.IsCancellationRequested)
            {
                var pipeServer = new NamedPipeServerStream(
                    this._pipeName,
                    PipeDirection.InOut,
                    this._maxNumberOfServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await pipeServer.WaitForConnectionAsync(this._cancellationTokenSource.Token);

                var clientId = Guid.NewGuid();
                var connectedClient = new ConnectedClient(clientId, pipeServer);
                this._connectedClients[clientId] = connectedClient;
                this.Connected?.Invoke(this, new StartServerEventArgs(pipeServer, this._connectedClients, clientId, this._pipeName));

                _ = HandleClientCommunicationAsync(connectedClient, this._connectedClients, this._cancellationTokenSource, this._pipeName, this._chunkSize)
                    .ContinueWith(task =>
                    {
                        if (task.Exception != null)
                        {
                            // Handle execptions
                            Console.WriteLine($"Client communication error: {task.Exception.InnerException?.Message}");
                        }
                    }, TaskContinuationOptions.NotOnFaulted);
            }
        }

        private async Task HandleClientCommunicationAsync(ConnectedClient client, ConcurrentDictionary<Guid, ConnectedClient> connectedClients, CancellationTokenSource cancellationTokenSource, string pipeName, int chunkSize)
        {

            try
            {
                this.EnterClientCommunication?.Invoke(this, client);

                var stream = client.MyStream as NamedPipeServerStream ?? throw new ArgumentNullException(nameof(client.MyStream)); ;
                while (stream.IsConnected)
                {
                    var data = await client.MyStream.ReceiveInChunksAsync(chunkSize);
                    if (data is null || data.Length == 0) continue;

                    this.ReceiveData?.Invoke(this, new ClientCommunicationEventArgs(client, data, pipeName, cancellationTokenSource, connectedClients));
                }
            }
            finally
            {
                connectedClients.TryRemove(client.Id, out _);
                client.MyStream.Close();
                client.MyStream.Dispose();
            }
        }

        public void Stop()
        {
            this.StopServer?.Invoke(this, new EventArgs());
            this._cancellationTokenSource.Cancel();
            return;
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
            this.Enter?.UnsubscribeAllHandlers<EventHandler> ((handler) => this.Enter -= handler);
            this.Connected?.UnsubscribeAllHandlers<StartServerEventHandler> ((handler) => this.Connected -= handler);
            this.EnterClientCommunication?.UnsubscribeAllHandlers<EventHandler<ConnectedClient>> ((handler) => this.EnterClientCommunication -= handler);
            this.ReceiveData?.UnsubscribeAllHandlers<ClientCommunicationEventHandler> ((handler) => this.ReceiveData -= handler);
            this.StopServer?.UnsubscribeAllHandlers<EventHandler> ((handler) => this.StopServer -= handler);
            return;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 관리형 상태(관리형 개체)를 삭제합니다.
                    this._cancellationTokenSource.Cancel();
                    this._cancellationTokenSource.Dispose();
                }

                // TODO: 비관리형 리소스(비관리형 개체)를 해제하고 종료자를 재정의합니다.
                // TODO: 큰 필드를 null로 설정합니다.
                this.DisconnectAllEvents();
                disposedValue = true;
            }
        }

        // // TODO: 비관리형 리소스를 해제하는 코드가 'Dispose(bool disposing)'에 포함된 경우에만 종료자를 재정의합니다.
        // ~PipeServer()
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
