using System.Diagnostics;
using Standard.DataType;
using Standard.Static;

namespace AsynchronousServer
{
    public static class ServerUtilities
    {
        internal static DateTime CurrentTime
        {
            get
            {
                return DateTime.Now;
            }
        }

        [Obsolete("구현 미흡", true)]
        public static void SendMessage (this IServer server, in Stream stream, Guid clientId, string message, StackTrace trace, Status status = Status.None)
        {
            if (server is null) throw new ArgumentNullException(nameof(server));
            if (stream is null) throw new ArgumentNullException(nameof(stream));

            var data = System.Text.Encoding.UTF8.GetBytes(Common.ToJson($"{CurrentTime} {message}", status.ToString()));
            server.SendInChunks(stream, data);
            return;
        }

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
