using System.Collections.Concurrent;
using AsynchronousServer.DataType;

namespace AsynchronousServer
{
    public class PipeServer
    {
        private readonly string _pipeName;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ConcurrentDictionary<Guid, ConnectedClient> _connectedClients;

        public PipeServer (string pipeName)
        {
            this._pipeName = pipeName;
            this._cancellationTokenSource = new CancellationTokenSource ();
            this._connectedClients = new ConcurrentDictionary<Guid, ConnectedClient>();
            return;
        }

        public async Task StartServerAsync()
        {

        }
    }
}
