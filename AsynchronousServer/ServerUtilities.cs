namespace AsynchronousServer
{
    public static class ServerUtilities
    {
        public static byte[]? ReceiveDataWithTimeout (this IServer server, Stream stream, int millisecondsTimeout, out Exception? outException)
        {
            if (server is null) throw new ArgumentNullException(nameof(server));
            if (stream is null) throw new ArgumentNullException(nameof(stream));

            byte[]? data = null;
            Exception? _exception = null;

            var task = Task.Run(() =>
            {
                try { data = server.ReceiveInChunks(stream); }
                catch (IOException exception) { _exception = exception; }
            });

            bool completed = task.Wait(millisecondsTimeout);
            if (!completed)
            {
                _exception = new TimeoutException("Data reception timeout exceeded. Please check the network connection and try again.");
            }

            outException = _exception;
            return data;
        }
    }
}
