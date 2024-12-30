namespace AsynchronousServer.DataType
{
    public class ConnectedClient
    {
        public Guid Id { get; private set; }
        public Stream MyStream { get; private set; }

        public ConnectedClient (Guid id, Stream stream)
        {
            this.Id = id;
            this.MyStream = stream;
            return;
        }
    }
}
