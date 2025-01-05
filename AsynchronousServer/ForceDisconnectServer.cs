using System.Text.Json;
using Standard.DataType;
using Standard.Static;

namespace AsynchronousServer
{
    public abstract class ForceDisconnectServer
    {
        protected abstract string ShutdownString { get; set; }

        public bool ForceClientDisconnect (IServer server, Stream stream, DataType.ConnectedClient client, int millisecondsTimeout = 3000)
        {
            var data = System.Text.Encoding.UTF8.GetBytes(Common.ToJson(string.Empty, this.ShutdownString));
            server.SendInChunks(stream, data);

            var buffer = server.ReceiveInChunks(stream, millisecondsTimeout, out var exception);
            if (buffer is null || exception != null)
            {
                server.Kill(client.Id);
                return false;
            }

            return server.Disconnect(client.Id);
        }
    }
}
