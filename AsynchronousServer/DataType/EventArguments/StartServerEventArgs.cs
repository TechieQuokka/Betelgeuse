using System.Collections.Concurrent;

namespace AsynchronousServer.DataType
{
    public class StartServerEventArgs
    {
        public Stream MyStream { get; private set; }
        public ConcurrentDictionary<Guid, ConnectedClient> Clients { get; private set; }
        public Guid ClientId { get; private set; }
        public string pipeName { get; private set; }

        public StartServerEventArgs(Stream stream, ConcurrentDictionary<Guid, ConnectedClient> clients, Guid clientId, string name)
        {
            this.MyStream = stream;
            this.Clients = clients;
            this.ClientId = clientId;
            this.pipeName = name;
            return;
        }
    }
}
