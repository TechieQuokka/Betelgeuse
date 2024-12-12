using System.Collections.Concurrent;
using System.IO.Pipes;

namespace AsynchronousServer.DataType
{
    public class StartServerEventArgs
    {
        public NamedPipeServerStream Stream { get; private set; }
        public ConcurrentDictionary<Guid, ConnectedClient> Clients { get; private set; }
        public Guid ClientId { get; private set; }
        public string pipeName { get; private set; }

        public StartServerEventArgs(NamedPipeServerStream stream, ConcurrentDictionary<Guid, ConnectedClient> clients, Guid clientId, string name)
        {
            this.Stream = stream;
            this.Clients = clients;
            this.ClientId = clientId;
            this.pipeName = name;
            return;
        }
    }
}
