using System.Net;
using System.Net.Sockets;
using AsynchronousServer;
using Betelgeuse.Global;
using MySql.Data.MySqlClient;

namespace Betelgeuse
{
    public partial class MainProgram
    {
        private static readonly MySqlConnection _sqlConnection = new MySqlConnection(DatabaseVariable.connectionString);
        private static readonly Security.IAdvancedEncryptionStandard AES = new Security.AdvancedEncryptionStandard(PrivateKey.aesKey);

        public static async Task Main(string[] args)
        {
            IServer pipeServer = new PipeServer("BetelgeuseLocalServer");
            InitializeLogin(pipeServer);

            IServer tcpServer = new TcpServer(IPAddress.Any, 32983);

            var connection = _sqlConnection ?? throw new ArgumentNullException(nameof(_sqlConnection));


            pipeServer.Enter += PipeServer_Enter;
            tcpServer.Enter += TcpServer_Enter;
            try
            {
                connection.Open();
                Console.WriteLine("DB has been connected!");
                var pipeServerTask = pipeServer.StartServerAsync();
                var tcpServerTask = tcpServer.StartServerAsync();
                var waitForExitTask = WaitForExitKeyAsync(pipeServer, tcpServer);

                await Task.WhenAny(pipeServerTask, tcpServerTask, waitForExitTask);
            }
            finally
            {
                AES.Dispose();
                connection?.Close();
                connection?.Dispose();
                pipeServer?.Stop();
                tcpServer?.Stop();
                tcpServer?.Dispose();
                pipeServer?.Dispose();
            }
            return;
        }

        private static void PipeServer_Enter(object? sender, EventArgs e)
        {
            Console.WriteLine("The server1 has started!");
            return;
        }

        private static void TcpServer_Enter(object? sender, EventArgs e)
        {
            Console.WriteLine("The server2 has started!");
            return;
        }

        private static async Task WaitForExitKeyAsync(IServer server1, IServer server2)
        {
            Console.WriteLine("Press the Delete key to shut down the server...");
            await Task.Run(() => { while (Console.ReadKey(true).Key != ConsoleKey.Delete) ; });

            server1.Stop();
            server2.Stop();
            Console.WriteLine("Shutting down the server...");
        }
    }
}
