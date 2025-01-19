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
        private readonly IDictionary<Guid, ConnectedClient> _connectedClients;
        private readonly IDictionary<Guid, TaskThreadPair> _tasks;
        private readonly int _chunkSize;
        private readonly IPAddress _ipAddress;
        private readonly int _port;
        private readonly int _timeout;
        private byte[] _buffer;
        private bool disposedValue;
        private object _lockObject = new object();

        int IServer.ConnectedClientCount => this._connectedClients.Count;
        IDictionary<Guid, ConnectedClient> IServer.ConnectedClients { get => this._connectedClients; }
        public int ChunkSize => this._chunkSize;

        public event EventHandler? Enter;
        public event StartServerEventHandler? Connected;
        public event EventHandler<ConnectedClient>? EnterClientCommunication;
        public event ClientCommunicationEventHandler? ReceiveData;
        public event ClientCommunicationEventHandler? DisconnectClient;
        public event EventHandler? StopServer;

        public TcpServer (IPAddress ipAddress, int port, int timeout = Timeout.Infinite, int chunkSize = 65536)
        {
            this._cancellationTokenSource = new CancellationTokenSource();
            this._connectedClients = new ConcurrentDictionary<Guid, ConnectedClient>();
            this._tasks = new ConcurrentDictionary<Guid, TaskThreadPair>();
            this._chunkSize = chunkSize;
            this._ipAddress = ipAddress;
            this._port = port;
            this._buffer = new byte[chunkSize];
            this._timeout = timeout;
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
                    var connectedClient = new ConnectedClient(clientId, client.GetStream(), new CancellationTokenSource());
                    this._connectedClients[clientId] = connectedClient;
                    this.Connected?.Invoke(this, new StartServerEventArgs(connectedClient.MyStream, this._connectedClients, clientId, string.Empty));

                    var tasks = this._tasks;
                    tasks.Add(clientId, new TaskThreadPair());
                    tasks[clientId].Task = Task.Factory.StartNew(async () =>
                    {
                        await HandleClientCommunicationAsync(client, connectedClient, this._connectedClients, tasks, this._cancellationTokenSource, this._chunkSize, this._timeout)
                        .ContinueWith(task =>
                        {
                            if (task.Exception != null)
                            {
                                Console.WriteLine($"Client communication error: {task.Exception.InnerException?.Message}");
                            }
                        }, TaskContinuationOptions.OnlyOnRanToCompletion);
                    }, TaskCreationOptions.LongRunning);
                }

                server.Stop();
            }
        }

        private async Task HandleClientCommunicationAsync (TcpClient client, ConnectedClient connectedClient, IDictionary<Guid, ConnectedClient> connectedClients, IDictionary<Guid, TaskThreadPair> tasks, CancellationTokenSource cancellationTokenSource, int chunkSize, int millisecondsDelay)
        {
            tasks[connectedClient.Id].CurrentThread = Thread.CurrentThread;
            try
            {
                this.EnterClientCommunication?.Invoke(this, connectedClient);

                while (client.Connected && !connectedClients[connectedClient.Id].Cancellation.Token.IsCancellationRequested)
                {
                    var receiveTokenSource = new CancellationTokenSource();
                    var disconnectionTokenSource = new CancellationTokenSource();
                    var receiveTask = connectedClient.MyStream.ReceiveInChunksAsync(chunkSize, receiveTokenSource);
                    var disconnectionTask = Task.Delay(millisecondsDelay, disconnectionTokenSource.Token);

                    var completedTask = await Task.WhenAny(receiveTask, disconnectionTask);
                    if (completedTask == disconnectionTask)
                    {
                        disconnectionTask.Dispose();
                        receiveTokenSource.Cancel();
                        await ForceTask(receiveTask);
                        return;
                    }
                    else if (receiveTask.Exception != null || receiveTask.Result is null || receiveTask.Result.Length is 0)
                    {
                        this.DisconnectClient?.Invoke(this, new ClientCommunicationEventArgs(connectedClient, [], string.Empty, cancellationTokenSource, connectedClients));
                        receiveTask.Dispose();
                        disconnectionTokenSource.Cancel();
                        await ForceTask(disconnectionTask);
                        return;
                    }

                    this.ReceiveData?.Invoke(this, new ClientCommunicationEventArgs(connectedClient, receiveTask.Result, string.Empty, cancellationTokenSource, connectedClients));
                    receiveTask.Dispose();
                    disconnectionTokenSource.Cancel();
                    await ForceTask(disconnectionTask);
                }
            }
            finally
            {
                (connectedClients as ConcurrentDictionary<Guid, ConnectedClient>)?.TryRemove(connectedClient.Id, out _);
                (tasks as ConcurrentDictionary<Guid, TaskThreadPair>)?.TryRemove(connectedClient.Id, out _);
                connectedClient.MyStream.Close();
                connectedClient.MyStream.Dispose();
                connectedClient.Cancellation.Dispose();
                client.Close();
                client.Dispose();
            }
            return;
            async Task ForceTask(Task task)
            {
                try { await task; }
                catch (TaskCanceledException)
                {
                    if (task.Status == TaskStatus.RanToCompletion || task.Status == TaskStatus.Faulted || task.Status == TaskStatus.Canceled)
                    {
                        task.Dispose();
                    }
                }
            }
        }

        public bool Disconnect(Guid clientId)
        {
            lock (this._lockObject)
            {
                var clients = this._connectedClients;
                if (clients.ContainsKey(clientId) is false) return false;

                clients[clientId].Cancellation.Cancel();
                return true;
            }
        }

        public bool Kill(Guid clientId)
        {
            lock (this._lockObject)
            {
                var tasks = this._tasks;
                if (tasks.ContainsKey(clientId) is false) return false;

                var task = tasks[clientId];
                bool result = task.Interrupt();
                task.Task?.Wait(this._timeout);

                return result && tasks.Remove(clientId);
            }
        }

        public void Stop()
        {
            lock (this._lockObject)
            {
                var tasks = this._tasks;
                foreach (var task in tasks)
                {
                    task.Value.Interrupt();
                }
                tasks.Clear();
            }

            this.StopServer?.Invoke(this, new EventArgs());
            this._cancellationTokenSource?.Cancel();
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
            this.Enter.UnsubscribeAllHandlers<EventHandler>((handler) => this.Enter -= handler);
            this.Connected.UnsubscribeAllHandlers<StartServerEventHandler>((handler) => this.Connected -= handler);
            this.EnterClientCommunication.UnsubscribeAllHandlers<EventHandler<ConnectedClient>>((handler) => this.EnterClientCommunication -= handler);
            this.ReceiveData.UnsubscribeAllHandlers<ClientCommunicationEventHandler>((handler) => this.ReceiveData -= handler);
            this.DisconnectClient.UnsubscribeAllHandlers<ClientCommunicationEventHandler>((header) => this.DisconnectClient -= header);
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
                    this.Stop();
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
