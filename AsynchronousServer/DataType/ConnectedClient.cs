namespace AsynchronousServer.DataType
{
    public sealed class ConnectedClient
    {
        public Guid Id { get; private set; }
        public Stream MyStream { get; private set; }
        public CancellationTokenSource Cancellation { get; private set; }

        public ConnectedClient (Guid id, Stream stream, CancellationTokenSource cancellation)
        {
            this.Id = id;
            this.MyStream = stream;
            this.Cancellation = cancellation;
            return;
        }
    }
}
