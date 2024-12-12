using System.Collections.Concurrent;
using System.IO.Pipes;
using AsynchronousServer.DataType;
using AsynchronousServer.StaticMethod;

namespace AsynchronousServer
{
    public delegate void StartServerEventHandler(object sender, StartServerEventArgs argument);
    public delegate void ClientCommunicationEventHandler(object sender, ClientCommunicationEventArgs argument);

    public class PipeServer
    {
        private readonly string _pipeName;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ConcurrentDictionary<Guid, ConnectedClient> _connectedClients;
        private readonly int _chunkSize;

        public event EventHandler<string>? Enter;
        public event StartServerEventHandler? Connected;
        public event EventHandler<ConnectedClient>? EnterClientCommunication;
        public event ClientCommunicationEventHandler? ReceiveData;

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

                    _ = HandleClientCommunicationAsync(connectedClient, this._chunkSize);
                }
                finally
                {
                    pipeServer.Close();
                    pipeServer.Dispose();
                }
            }
        }

        private async Task HandleClientCommunicationAsync(ConnectedClient client, int chunkSize)
        {
            try
            {
                this.EnterClientCommunication?.Invoke(this, client);

                while (client.PipeStream.IsConnected)
                {
                    var data = await client.PipeStream.ReceiveInChunksAsync(chunkSize);
                    if (data is null || data.Length == 0) continue;

                    this.ReceiveData?.Invoke(this, new ClientCommunicationEventArgs(client, data, this._pipeName, this._cancellationTokenSource, this._connectedClients));
                }
            }
            finally
            {
                client.PipeStream.Close();
                client.PipeStream.Dispose();
            }
        }
    }
}
