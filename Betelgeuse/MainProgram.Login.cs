using System.Text.Json;
using AsynchronousServer;
using Betelgeuse.Global;
using Standard.DataType;
using Standard.Static;

namespace Betelgeuse
{
    public partial class MainProgram
    {
        private static void InitializeLogin(IServer server)
        {
            server.EnterClientCommunication += IntegrateApplication_EventCallback;
            return;
        }

        private static void IntegrateApplication_EventCallback(object? sender, AsynchronousServer.DataType.ConnectedClient arguments)
        {
            var pipeServer = sender as IServer ?? throw new ArgumentNullException(nameof(sender));
            var disconnect = sender as ForceDisconnectServer ?? throw new ArgumentNullException(nameof(sender));
            var stream = arguments.MyStream;

            string commandString = "Integrate";

            var data = System.Text.Encoding.UTF8.GetBytes(Common.ToJson(string.Empty, commandString));
            pipeServer.SendInChunks(arguments.MyStream, data);

            var buffer = pipeServer.ReceiveInChunks(stream);
            string jsonString = System.Text.Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            var header = JsonSerializer.Deserialize<Header>(jsonString);
            if (header == null || header.Request != commandString)
            {
                _ = disconnect.ForceClientDisconnect(pipeServer, stream, arguments);
                // logging...
                return;
            }

            var key = JsonSerializer.Deserialize<byte[]>(header.Data);
            if (key == null || key.Length == 0)
            {
                _ = disconnect.ForceClientDisconnect(pipeServer, stream, arguments);
                // logging...
                return;
            }

            var aesKey = AES.Decrypt(key);
            if (aesKey != PrivateKey.integrateKey)
            {
                _ = disconnect.ForceClientDisconnect(pipeServer, stream, arguments);
                // logging...
                return;
            }

            // successful!!
            // logging...

            data = System.Text.Encoding.UTF8.GetBytes(Common.ToJson("Successful!", commandString));
            pipeServer.SendInChunks(stream, data);

            Console.WriteLine("Successful!!");
            return;
        }
    }
}
