namespace AsynchronousServer
{
    public static class ServerUtilities
    {
        public static byte[]? ReceiveDataWithTimeout (this IServer server, Stream stream, int millisecondsTimeout, out ThreadInterruptedException? outException)
        {
            if (server is null) throw new ArgumentNullException(nameof(server));
            if (stream is null) throw new ArgumentNullException(nameof(stream));

            byte[]? data = null;

            ThreadInterruptedException? _exception = null;
            var thread = new Thread(new ThreadStart(() =>
            {
                try { data = server.ReceiveInChunks(stream); }
                catch (ThreadInterruptedException exception) { _exception = exception; }
            }));

            thread.Start();
            bool completed = thread.Join(millisecondsTimeout);
            if (completed is false) thread.Interrupt();

            outException = _exception;
            return data;
        }
    }
}
