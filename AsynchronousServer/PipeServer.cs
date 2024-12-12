using System.Collections.Concurrent;
using System.IO.Pipes;
using AsynchronousServer.DataType;
using AsynchronousServer.StaticMethod;

namespace AsynchronousServer
{
    public delegate void StartServerEventHandler(object sender, StartServerEventArgs argument);
    public delegate void ClientCommunicationEventHandler(object sender, ClientCommunicationEventArgs argument);

    public class PipeServer : IDisposable
    {
        private readonly string _pipeName;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ConcurrentDictionary<Guid, ConnectedClient> _connectedClients;
        private readonly int _chunkSize;
        private bool disposedValue;

        public event EventHandler<string>? Enter;
        public event StartServerEventHandler? Connected;
        public event EventHandler<ConnectedClient>? EnterClientCommunication;
        public event ClientCommunicationEventHandler? ReceiveData;
        public event EventHandler? StopServer;

        public int ConnectedClientCount { get => this._connectedClients.Count; }

        public PipeServer (string pipeName, int chunkSize = 65536)
        {
            this._pipeName = pipeName;
            this._cancellationTokenSource = new CancellationTokenSource ();
            this._connectedClients = new ConcurrentDictionary<Guid, ConnectedClient>();
            this._chunkSize = chunkSize;
            return;
        }

        public async Task StartServerAsync(int maxNumberOfServerInstances = 10)
        {
            this.Enter?.Invoke(this, this._pipeName);

            while (!this._cancellationTokenSource.Token.IsCancellationRequested)
            {
                var pipeServer = new NamedPipeServerStream(
                    this._pipeName,
                    PipeDirection.InOut,
                    maxNumberOfServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);
                try
                {
                    await pipeServer.WaitForConnectionAsync(this._cancellationTokenSource.Token);

                    var clientId = Guid.NewGuid();
                    var connectedClient = new ConnectedClient(clientId, pipeServer);
                    this._connectedClients[clientId] = connectedClient;
                    this.Connected?.Invoke(this, new StartServerEventArgs(pipeServer, this._connectedClients, clientId, this._pipeName));

                    _ = HandleClientCommunicationAsync(connectedClient, this._connectedClients, this._cancellationTokenSource, this._pipeName, this._chunkSize);
                }
                finally
                {
                    pipeServer.Close();
                    pipeServer.Dispose();
                }
            }
        }

        private async Task HandleClientCommunicationAsync(ConnectedClient client, ConcurrentDictionary<Guid, ConnectedClient> connectedClients, CancellationTokenSource cancellationTokenSource, string pipeName, int chunkSize)
        {
            try
            {
                this.EnterClientCommunication?.Invoke(this, client);

                while (client.PipeStream.IsConnected)
                {
                    var data = await client.PipeStream.ReceiveInChunksAsync(chunkSize);
                    if (data is null || data.Length == 0) continue;

                    this.ReceiveData?.Invoke(this, new ClientCommunicationEventArgs(client, data, pipeName, cancellationTokenSource, connectedClients));
                }
            }
            finally
            {
                connectedClients.TryRemove(client.Id, out _);
                client.PipeStream.Close();
                client.PipeStream.Dispose();
            }
        }

        public void Stop()
        {
            this.StopServer?.Invoke(this, new EventArgs());
            this._cancellationTokenSource.Cancel();
            return;
        }

        private void DisconnectAllEvents()
        {
            // 모든 핸들러 해제
            if (this.Enter != null)
            {
                foreach (var handler in this.Enter.GetInvocationList())
                {
                    this.Enter -= (EventHandler<string>)handler;
                }
            }

            if (this.Connected != null)
            {
                foreach (var handler in this.Connected.GetInvocationList())
                {
                    this.Connected -= (StartServerEventHandler)handler;
                }
            }

            if (this.EnterClientCommunication != null)
            {
                foreach (var handler in this.EnterClientCommunication.GetInvocationList())
                {
                    this.EnterClientCommunication -= (EventHandler<ConnectedClient>)handler;
                }
            }

            if (this.ReceiveData != null)
            {
                foreach (var handler in this.ReceiveData.GetInvocationList())
                {
                    this.ReceiveData -= (ClientCommunicationEventHandler)handler;
                }
            }

            if (this.StopServer != null)
            {
                foreach (var handler in this.StopServer.GetInvocationList())
                {
                    this.StopServer -= (EventHandler)handler;
                }
            }
            return;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 관리형 상태(관리형 개체)를 삭제합니다.
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
