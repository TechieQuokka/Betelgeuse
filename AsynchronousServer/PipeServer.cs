using System.Collections.Concurrent;
using System.IO.Pipes;
using AsynchronousServer.DataType;
using AsynchronousServer.StaticMethod;
using static Standard.Static.Free;

namespace AsynchronousServer
{
    public delegate void StartServerEventHandler(object sender, StartServerEventArgs argument);
    public delegate void ClientCommunicationEventHandler(object sender, ClientCommunicationEventArgs argument);

    public class PipeServer : ForceDisconnectServer, IServer
    {
        private readonly string _pipeName;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly IDictionary<Guid, ConnectedClient> _connectedClients;
        private readonly IDictionary<Guid, TaskThreadPair> _tasks;
        private readonly int _chunkSize;
        private readonly int _maxNumberOfServerInstances;
        private readonly int _timeout;
        private bool disposedValue;
        private byte[] _buffer;
        private string _shutdownString = "SHUTDOWN";

        public event EventHandler? Enter;
        public event StartServerEventHandler? Connected;
        public event EventHandler<ConnectedClient>? EnterClientCommunication;
        public event ClientCommunicationEventHandler? ReceiveData;
        public event ClientCommunicationEventHandler? DisconnectClient;
        public event EventHandler? StopServer;

        public int ChunkSize => this._chunkSize;

        int IServer.ConnectedClientCount { get => this._connectedClients.Count; }
        IDictionary<Guid, ConnectedClient> IServer.ConnectedClients { get => this._connectedClients; }
        protected override string ShutdownString { get => this._shutdownString; set => this._shutdownString = value; }

        public PipeServer (string pipeName, int maxNumberOfServerInstances = 10, int timeout = Timeout.Infinite, int chunkSize = 65536)
        {
            this._pipeName = pipeName;
            this._cancellationTokenSource = new CancellationTokenSource();
            this._connectedClients = new ConcurrentDictionary<Guid, ConnectedClient>();
            this._tasks = new ConcurrentDictionary<Guid, TaskThreadPair>();
            this._chunkSize = chunkSize;
            this._maxNumberOfServerInstances = maxNumberOfServerInstances;
            this._buffer = new byte[chunkSize];
            this._timeout = timeout;
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
                var connectedClient = new ConnectedClient(clientId, pipeServer, new CancellationTokenSource());
                this._connectedClients[clientId] = connectedClient;
                this.Connected?.Invoke(this, new StartServerEventArgs(pipeServer, this._connectedClients, clientId, this._pipeName));

                var tasks = this._tasks;
                tasks.Add(clientId, new TaskThreadPair());
                tasks[clientId].Task = HandleClientCommunicationAsync(connectedClient, this._connectedClients, tasks, this._cancellationTokenSource, this._pipeName, this._chunkSize, this._timeout)
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

        private async Task HandleClientCommunicationAsync(ConnectedClient client, IDictionary<Guid, ConnectedClient> connectedClients, IDictionary<Guid, TaskThreadPair> tasks, CancellationTokenSource cancellationTokenSource, string pipeName, int chunkSize, int millisecondsDelay)
        {
            tasks[client.Id].CurrentThread = Thread.CurrentThread;
            try
            {
                this.EnterClientCommunication?.Invoke(this, client);

                var stream = client.MyStream as NamedPipeServerStream ?? throw new ArgumentNullException(nameof(client.MyStream));
                while (stream.IsConnected && !client.Cancellation.Token.IsCancellationRequested)
                {
                    var receiveTask = client.MyStream.ReceiveInChunksAsync(chunkSize);
                    var disconnectionTask = this.WaitingForDisconnection(client.Cancellation, millisecondsDelay);

                    var completedTask = await Task.WhenAny(receiveTask, disconnectionTask);
                    if (completedTask == disconnectionTask)
                    {
                        receiveTask?.Dispose();
                        return;
                    }
                    else if (receiveTask.Exception != null || receiveTask.Result is null || receiveTask.Result.Length is 0)
                    {
                        this.DisconnectClient?.Invoke(this, new ClientCommunicationEventArgs(client, receiveTask.Result ?? [], pipeName, cancellationTokenSource, connectedClients));
                        return;
                    }

                    this.ReceiveData?.Invoke(this, new ClientCommunicationEventArgs(client, receiveTask.Result, pipeName, cancellationTokenSource, connectedClients));
                    disconnectionTask?.Dispose();
                }
            }
            finally
            {
                (connectedClients as ConcurrentDictionary<Guid, ConnectedClient>)?.TryRemove(client.Id, out _);
                (tasks as ConcurrentDictionary<Guid, TaskThreadPair>)?.TryRemove(client.Id, out _);
                client.MyStream.Close();
                client.MyStream.Dispose();
                client.Cancellation.Dispose();
            }
        }

        private async Task WaitingForDisconnection(CancellationTokenSource cancellation, int millisecondsDelay)
        {
            await Task.Delay(millisecondsDelay, cancellation.Token);
            return;
        }

        public bool Disconnect(Guid clientId)
        {
            var clients = this._connectedClients;
            if (clients.ContainsKey(clientId) is false) return false;

            clients[clientId].Cancellation.Cancel();
            return true;
        }

        public bool Kill (Guid clientId)
        {
            var tasks = this._tasks;
            if (tasks.ContainsKey(clientId) is false) return false;

            var task = tasks[clientId];
            bool result = task.Interrupt();
            task.Task?.Wait();

            return result && tasks.Remove(clientId);
        }

        public void Stop()
        {
            var tasks = this._tasks;
            foreach (var task in tasks)
            {
                task.Value.Interrupt();
            }
            tasks.Clear();

            this.StopServer?.Invoke(this, new EventArgs());
            this._cancellationTokenSource.Cancel();
            return;
        }

        public void SendInChunks(in Stream stream, byte[] data)
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

        public byte[] ReceiveInChunks(in Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            var dataSizeBuffer = new byte[sizeof(int)];
            int byteRead = stream.Read(dataSizeBuffer, 0, dataSizeBuffer.Length);
            if (byteRead != dataSizeBuffer.Length)
            {
                throw new InvalidOperationException("Failed to read data size.");
            }
            int dataSize = BitConverter.ToInt32(dataSizeBuffer, 0);

            var receivedData = new List<byte>();
            var buffer = this._buffer;
            int bytesRead = 0;
            int totalBytesRead = 0;

            while (totalBytesRead < dataSize && (bytesRead = stream.Read(buffer, 0, this._chunkSize)) > 0)
            {
                receivedData.AddRange(buffer.Take(bytesRead));
                totalBytesRead += bytesRead;
            }

            if (totalBytesRead != dataSize)
            {
                throw new InvalidOperationException("Failed to read the complete data.");
            }
            return receivedData.ToArray();
        }

        private void DisconnectAllEvents()
        {
            this.Enter?.UnsubscribeAllHandlers<EventHandler> ((handler) => this.Enter -= handler);
            this.Connected?.UnsubscribeAllHandlers<StartServerEventHandler> ((handler) => this.Connected -= handler);
            this.EnterClientCommunication?.UnsubscribeAllHandlers<EventHandler<ConnectedClient>> ((handler) => this.EnterClientCommunication -= handler);
            this.ReceiveData?.UnsubscribeAllHandlers<ClientCommunicationEventHandler> ((handler) => this.ReceiveData -= handler);
            this.DisconnectClient?.UnsubscribeAllHandlers<ClientCommunicationEventHandler> ((handler) => this.DisconnectClient -= handler);
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
                    this.Stop();
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
