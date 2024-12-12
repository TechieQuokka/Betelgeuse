using System.Collections.Concurrent;
using System.IO.Pipes;
using AsynchronousServer.DataType;

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

                await pipeServer.WaitForConnectionAsync(this._cancellationTokenSource.Token);

                var clientId = Guid.NewGuid();
                this._connectedClients[clientId] = new ConnectedClient(clientId, pipeServer);
                this.Connected?.Invoke(this, new StartServerEventArgs(pipeServer, this._connectedClients, clientId, this._pipeName));


            }
        }

        private async Task HandleClientCommunicationAsync (ConnectedClient client)
        {

        }
    }
}
