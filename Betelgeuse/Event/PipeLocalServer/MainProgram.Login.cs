using System.Text.Json;
using AsynchronousServer;
using Betelgeuse.Extension;
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
            server.DisconnectClient += Server_DisconnectClient;
            return;
        }

        private static void IntegrateApplication_EventCallback(object? sender, AsynchronousServer.DataType.ConnectedClient client)
        {
            var pipeServer = sender as IServer ?? throw new ArgumentNullException(nameof(sender));
            var disconnect = sender as ForceDisconnectServer ?? throw new ArgumentNullException(nameof(sender));
            var stream = client.MyStream;
            var identifier = _identifier;

            string commandString = "Integrate";

            var data = System.Text.Encoding.UTF8.GetBytes(Common.ToJson(string.Empty, commandString));
            pipeServer.SendInChunks(client.MyStream, data); // 1

            int timeout = 60 * 1000;
            var buffer = pipeServer.ReceiveDataWithTimeout(stream, timeout, out var exception); // 4
            if (buffer is null || exception != null)
            {
                var message = "Data reception timeout exceeded. Please check the network connection and try again.";
                client.MyStream.SendDataWithLogging(pipeServer, client.Id, message, new System.Diagnostics.StackTrace(), Status.Error);
                DisconnectAndTerminateClient(pipeServer, client.Id);
                return;
            }

            string jsonString = System.Text.Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            var header = JsonSerializer.Deserialize<Header>(jsonString);
            if (header == null || header.Request != commandString)
            {
                var message = "Failed to deserialize header. The received data may be corrupted or incomplete. Please check the data and try again.";
                client.MyStream.SendDataWithLogging(pipeServer, client.Id, message, new System.Diagnostics.StackTrace(), Status.Error);
                DisconnectAndTerminateClient(pipeServer, client.Id);
                return;
            }

            var authData = JsonSerializer.Deserialize<DataType.Login.AuthenticationData>(header.Data);
            if (authData == null || authData.Key == null)
            {
                var message = "Failed to deserialize authentication data. The received data may be corrupted or incomplete. Please check the data and try again.";
                client.MyStream.SendDataWithLogging(pipeServer, client.Id, message, new System.Diagnostics.StackTrace(), Status.Error);
                DisconnectAndTerminateClient(pipeServer, client.Id);
                return;
            }

            var aesKey = AES.Decrypt(authData.Key);
            if (aesKey != PrivateKey.integrateKey)
            {
                var message = "Failed to decrypt the authentication key. The received data may be corrupted or incorrect. Please try again.";
                client.MyStream.SendDataWithLogging(pipeServer, client.Id, message, new System.Diagnostics.StackTrace(), Status.Error);
                disconnect.ForceClientDisconnect(pipeServer, stream, client, timeout);
                return;
            }

            // successful!!
            var result = identifier.Add(client.Id, authData.Name.ToLower());
            if (result is false)
            {
                var message = "Failed to add client identifier. The username already exists. Please choose a different username.";
                client.MyStream.SendDataWithLogging(pipeServer, client.Id, message, new System.Diagnostics.StackTrace(), Status.Error);
                disconnect.ForceClientDisconnect(pipeServer, stream, client, timeout);
                return;
            }
            {
                var message = "Client successfully authenticated and added to the identifier list.";
                client.MyStream.SendDataWithLogging(pipeServer, client.Id, message, new System.Diagnostics.StackTrace(), Status.Successful); // 5
            }
            return;

            void DisconnectAndTerminateClient(IServer server, Guid clientId)
            {
                server.Disconnect(clientId);
                Thread.Sleep(10);
                server.Kill(clientId);
                return;
            }
        }

        private static void Server_DisconnectClient(object sender, AsynchronousServer.DataType.ClientCommunicationEventArgs argument)
        {
            var identifier = _identifier;
            identifier.Remove(argument.Client.Id);
            Console.WriteLine($"Client {argument.Client.Id} has been disconnected.");
            return;
        }
    }
}
