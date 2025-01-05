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

        /*
         TODO: ReceiveInChunks() 비동기적으로 바꿀 것!
         TODO: 클라이언트 이름 받을 것!
         */
        private static void IntegrateApplication_EventCallback(object? sender, AsynchronousServer.DataType.ConnectedClient client)
        {
            var pipeServer = sender as IServer ?? throw new ArgumentNullException(nameof(sender));
            var disconnect = sender as ForceDisconnectServer ?? throw new ArgumentNullException(nameof(sender));
            var stream = client.MyStream;

            string commandString = "Integrate";

            var data = System.Text.Encoding.UTF8.GetBytes(Common.ToJson(string.Empty, commandString));
            pipeServer.SendInChunks(client.MyStream, data);

            int timeout = 60 * 1000;
            var buffer = pipeServer.ReceiveInChunks(stream, timeout, out var exception);
            if (buffer is null || exception != null)
            {
                pipeServer.Kill(client.Id);
                // logging...
                return;
            }

            string jsonString = System.Text.Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            var header = JsonSerializer.Deserialize<Header>(jsonString);
            if (header == null || header.Request != commandString)
            {
                pipeServer.Kill(client.Id);
                // logging...
                return;
            }

            var key = JsonSerializer.Deserialize<byte[]>(header.Data);
            if (key == null || key.Length == 0)
            {
                pipeServer.Kill(client.Id);
                // logging...
                return;
            }

            var aesKey = AES.Decrypt(key);
            if (aesKey != PrivateKey.integrateKey)
            {
                disconnect.ForceClientDisconnect(pipeServer, stream, client, 3000);
                // logging...
                return;
            }

            // successful!!
            // logging...

            data = System.Text.Encoding.UTF8.GetBytes(Common.ToJson("Successful", commandString));
            pipeServer.SendInChunks(stream, data);

            Console.WriteLine("Successful!!");
            return;
        }
    }
}
