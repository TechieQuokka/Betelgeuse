using System.Collections.Concurrent;
using System.Text.Json;
using AsynchronousServer;
using AsynchronousServer.DataType;
using Betelgeuse.Database.Workflow;
using Betelgeuse.Extension;
using Betelgeuse.Global;
using Standard.DataType;
using Standard.Static;

namespace Betelgeuse
{
    public partial class MainProgram
    {
        private static IDictionary<string, Guid> _identityGroup = new ConcurrentDictionary<string, Guid>();

        private static void InitializeLogin(IServer pipeServer, IServer tcpServer)
        {
            pipeServer.EnterClientCommunication += IntegrateApplication_EventCallback;
            pipeServer.DisconnectClient += Server_DisconnectClient;

            tcpServer.EnterClientCommunication += ConnectServer_EventCallback;
            tcpServer.DisconnectClient += TcpServer_DisconnectClient;
            return;
        }

        // TODO: 구현 미흡
        private static void ConnectServer_EventCallback(object? sender, ConnectedClient client)
        {
            var tcpServer = sender as IServer ?? throw new ArgumentNullException(nameof(sender));
            var stream = client.MyStream;
            var identityGroup = _identityGroup;
            var sql = _sqlConnection;

            string commandString = "RequestLogin", identity = string.Empty;

            int count = 0, maxAttempts = 3;
            for (count = 0; count < maxAttempts; count++)
            {
                var data = System.Text.Encoding.UTF8.GetBytes(Common.ToJson(string.Empty, commandString));
                tcpServer.SendInChunks(stream, data);

                int timeout = 60 * 1000;
                var buffer = tcpServer.ReceiveDataWithTimeout(stream, timeout, out var exception);
                if (buffer is null || exception != null)
                {
                    var message = "Data reception timeout exceeded. Please check the network connection and try again.";
                    client.MyStream.SendDataWithLogging(tcpServer, client.Id, message, new System.Diagnostics.StackTrace(), Status.Error);
                    DisconnectAndTerminateClient(tcpServer, client.Id);
                    return;
                }

                if (sql.IsLoginSuccessful(buffer, buffer.Length, commandString, out identity)) break;
                else stream.SendDataWithLogging(tcpServer, client.Id, $"Login failed: {client.Id}", new System.Diagnostics.StackTrace(), Status.Error);
            }
            if (count == maxAttempts)
            {
                var message = "You have exceeded the number of attempts.";
                client.MyStream.SendDataWithLogging(tcpServer, client.Id, message, new System.Diagnostics.StackTrace(), Status.Error);
                DisconnectAndTerminateClient(tcpServer, client.Id);
                return;
            }
            else if (identityGroup.ContainsKey(identity))
            {
                var message = "This account is already logged in. Please try again.";
                client.MyStream.SendDataWithLogging(tcpServer, client.Id, message, new System.Diagnostics.StackTrace(), Status.Error);
                DisconnectAndTerminateClient(tcpServer, client.Id);
                return;
            }

            // Successful
            {
                var message = "Server connection was successful.";
                stream.SendDataWithLogging(tcpServer, client.Id, message, new System.Diagnostics.StackTrace(), Status.Successful);
            }
            return;
        }

        private static void IntegrateApplication_EventCallback(object? sender, ConnectedClient client)
        {
            var pipeServer = sender as IServer ?? throw new ArgumentNullException(nameof(sender));
            var disconnect = sender as ForceDisconnectServer ?? throw new ArgumentNullException(nameof(sender));
            var stream = client.MyStream;
            var identifier = _identifier;

            string commandString = "Integrate";

            var data = System.Text.Encoding.UTF8.GetBytes(Common.ToJson(string.Empty, commandString));
            pipeServer.SendInChunks(stream, data); // 1

            int timeout = 60 * 1000;
            var buffer = pipeServer.ReceiveDataWithTimeout(stream, timeout, out var exception); // 4
            if (buffer is null || exception != null)
            {
                var message = "Data reception timeout exceeded. Please check the network connection and try again.";
                stream.SendDataWithLogging(pipeServer, client.Id, message, new System.Diagnostics.StackTrace(), Status.Error);
                DisconnectAndTerminateClient(pipeServer, client.Id);
                return;
            }

            string jsonString = System.Text.Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            var header = JsonSerializer.Deserialize<Header>(jsonString);
            if (header == null || header.Request != commandString)
            {
                var message = "Failed to deserialize header. The received data may be corrupted or incomplete. Please check the data and try again.";
                stream.SendDataWithLogging(pipeServer, client.Id, message, new System.Diagnostics.StackTrace(), Status.Error);
                DisconnectAndTerminateClient(pipeServer, client.Id);
                return;
            }

            var authData = JsonSerializer.Deserialize<DataType.Login.AuthenticationData>(header.Data);
            if (authData == null || authData.Key == null)
            {
                var message = "Failed to deserialize authentication data. The received data may be corrupted or incomplete. Please check the data and try again.";
                stream.SendDataWithLogging(pipeServer, client.Id, message, new System.Diagnostics.StackTrace(), Status.Error);
                DisconnectAndTerminateClient(pipeServer, client.Id);
                return;
            }

            var aesKey = AES.Decrypt(authData.Key);
            if (aesKey != PrivateKey.integrateKey)
            {
                var message = "Failed to decrypt the authentication key. The received data may be corrupted or incorrect. Please try again.";
                stream.SendDataWithLogging(pipeServer, client.Id, message, new System.Diagnostics.StackTrace(), Status.Error);
                disconnect.ForceClientDisconnect(pipeServer, stream, client, timeout);
                return;
            }

            // successful!!
            var result = identifier.Add(client.Id, authData.Name.ToLower());
            if (result is false)
            {
                var message = "Failed to add client identifier. The username already exists. Please choose a different username.";
                stream.SendDataWithLogging(pipeServer, client.Id, message, new System.Diagnostics.StackTrace(), Status.Error);
                disconnect.ForceClientDisconnect(pipeServer, stream, client, timeout);
                return;
            }
            {
                var message = "Client successfully authenticated and added to the identifier list.";
                stream.SendDataWithLogging(pipeServer, client.Id, message, new System.Diagnostics.StackTrace(), Status.Successful); // 5
            }
            return;
        }

        private static void Server_DisconnectClient(object sender, ClientCommunicationEventArgs argument)
        {
            var identifier = _identifier;
            identifier.Remove(argument.Client.Id);
            Console.WriteLine($"Client {argument.Client.Id} has been disconnected.");
            return;
        }

        private static void TcpServer_DisconnectClient(object sender, ClientCommunicationEventArgs argument)
        {
            Console.WriteLine($"Client {argument.Client.Id} has been disconnected.");
            return;
        }

        private static void DisconnectAndTerminateClient(IServer server, Guid clientId)
        {
            server.Disconnect(clientId);
            Thread.Sleep(10);
            server.Kill(clientId);
            return;
        }
    }
}
