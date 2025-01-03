using System.Text.Json;
using Standard.DataType;
using Standard.Static;

namespace AsynchronousServer
{
    public abstract class ForceDisconnectServer
    {
        protected abstract string ShutdownString { get; set; }

        public (Header? header, bool result) ForceClientDisconnect (IServer server, Stream stream, DataType.ConnectedClient client)
        {
            var data = System.Text.Encoding.UTF8.GetBytes(Common.ToJson(string.Empty, this.ShutdownString));
            server.SendInChunks(stream, data);

            var buffer = server.ReceiveInChunks(stream);
            string jsonString = System.Text.Encoding.UTF8.GetString(buffer);
            var header = JsonSerializer.Deserialize<Header>(jsonString);
            bool result = server.Disconnect(client.Id);
            return (header, result);
        }
    }
}
