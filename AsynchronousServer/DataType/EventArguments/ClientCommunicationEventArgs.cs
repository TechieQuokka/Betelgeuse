using System.Collections.Concurrent;

namespace AsynchronousServer.DataType
{
    public class ClientCommunicationEventArgs : EventArgs
    {
        public ConnectedClient Client { get; private set; }
        public byte[] Data { get; private set; }
        public string PipeName { get; private set; }
        public CancellationTokenSource CancellationTokenSource { get; private set; }
        public IDictionary<Guid, ConnectedClient> Clients { get; private set; }

        public ClientCommunicationEventArgs(ConnectedClient client, byte[] data, string name, CancellationTokenSource cancellationTokenSource, IDictionary<Guid, ConnectedClient> clients)
        {
            this.Client = client;
            this.Data = data;
            this.PipeName = name;
            this.CancellationTokenSource = cancellationTokenSource;
            this.Clients = clients;
            return;
        }
    }
}
