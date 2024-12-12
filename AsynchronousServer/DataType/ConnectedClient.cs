using System.IO.Pipes;

namespace AsynchronousServer.DataType
{
    public class ConnectedClient
    {
        public Guid Id { get; private set; }
        public NamedPipeServerStream PipeStream { get; private set; }

        public ConnectedClient (Guid id, NamedPipeServerStream pipeStream)
        {
            this.Id = id;
            this.PipeStream = pipeStream;
            return;
        }
    }
}
