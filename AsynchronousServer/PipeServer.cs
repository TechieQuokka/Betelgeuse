using System.Collections.Concurrent;
using System.IO.Pipes;
using AsynchronousServer.DataType;
using AsynchronousServer.StaticMethod;

namespace AsynchronousServer
{
    public delegate void StartServerEventHandler(object sender, StartServerEventArgs argument);

    public class PipeServer
    {
        private readonly string _pipeName;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ConcurrentDictionary<Guid, ConnectedClient> _connectedClients;
        
        public event EventHandler<string>? Enter;
        public event StartServerEventHandler? Connected;

        public PipeServer (string pipeName)
        {
            this._pipeName = pipeName;
            this._cancellationTokenSource = new CancellationTokenSource ();
            this._connectedClients = new ConcurrentDictionary<Guid, ConnectedClient>();
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

                    _ = HandleClientCommunicationAsync(connectedClient);
                }
                finally
                {
                    pipeServer.Close();
                    pipeServer.Dispose();
                }
            }
        }

        private async Task HandleClientCommunicationAsync(ConnectedClient client)
        {
            while (client.PipeStream.IsConnected)
            {
                await Network.ReceiveInChunksAsync(client.PipeStream as Stream, 10);
            }
        }
    }
}
