using System.Text.Json;
using AsynchronousServer;
using AsynchronousServer.StaticMethod;
using Betelgeuse.Global;
using Standard.DataType;
using Standard.Static;
using static Betelgeuse.Global.GlobalVariable;

namespace Betelgeuse
{
    public partial class MainProgram
    {
        private static void InitializeLogin(PipeServer server)
        {
            server.EnterClientCommunication += IntegrateApplication_EventCallback;
            return;
        }

        private static async void IntegrateApplication_EventCallback(object? sender, AsynchronousServer.DataType.ConnectedClient arguments)
        {
            var pipeServer = sender as PipeServer ?? throw new ArgumentNullException(nameof(sender));
            var stream = arguments.MyStream;

            string commandString = "Integrate";

            var data = System.Text.Encoding.UTF8.GetBytes(Common.ToJson(string.Empty, commandString));
            await stream.SendInChunksAsync(data, BufferSize);

            var buffer = await stream.ReceiveInChunksAsync (BufferSize);
            string jsonString = System.Text.Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            var header = JsonSerializer.Deserialize<Header>(jsonString);
            if (header == null || header.Request == commandString)
            {
                pipeServer.Stop();
                // logging...
                return;
            }

            var key = JsonSerializer.Deserialize<byte[]>(header.Data);
            if (key == null || key.Length == 0)
            {
                pipeServer.Stop();
                // logging...
                return;
            }

            var aesKey = AES.Decrypt(key);
            if (aesKey != PrivateKey.integrateKey)
            {
                pipeServer.Stop();
                // logging...
                return;
            }

            // successful!!
            // logging...
            return;
        }
    }
}
