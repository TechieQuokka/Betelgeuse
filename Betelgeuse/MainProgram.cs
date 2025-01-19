using System.Net;
using AsynchronousServer;
using Betelgeuse.DataType.Login;
using Betelgeuse.Global;
using log4net;
using log4net.Config;
using MySql.Data.MySqlClient;

namespace Betelgeuse
{
    internal partial class MainProgram
    {
        private static readonly MySqlConnection _sqlConnection = new MySqlConnection(DatabaseVariable.connectionString);
        private static readonly Security.IAdvancedEncryptionStandard AES = new Security.AdvancedEncryptionStandard(PrivateKey.aesKey);
        private static readonly ClientIdentifier _identifier = new ClientIdentifier();
        private static readonly ILog _log = LogManager.GetLogger(typeof(MainProgram));

        public static ILog Log { get => _log; }

        internal static async Task Main(string[] args)
        {
            IServer pipeServer = new PipeServer("BetelgeuseLocalServer", timeout: Timeout.Infinite);
            IServer tcpServer = new TcpServer(IPAddress.Any, 32983);

            InitializeLogin(pipeServer, tcpServer);
            InitializeTCPCommunication(tcpServer);

            var connection = _sqlConnection ?? throw new ArgumentNullException(nameof(_sqlConnection));

            pipeServer.Enter += (_, _) => Console.WriteLine("The Pipe server has started!");
            tcpServer.Enter += (_, _) => Console.WriteLine("The Tcp Server has started!");
            try
            {
                var log = Log ?? throw new ArgumentNullException(nameof(Log));
                Directory.CreateDirectory("ServerLog");
                XmlConfigurator.Configure(new FileInfo("log4net.config"));

                log.Info("Start program!");

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
                _identifier.Dispose();
            }
            return;
        }

        private static async Task WaitForExitKeyAsync(params IServer[] servers)
        {
            Console.WriteLine("Press the Delete key to shut down the server...");
            await Task.Run(() => { while (Console.ReadKey(true).Key != ConsoleKey.Delete) ; });

            foreach (var server in servers)
            {
                server.Stop();
            }
            Console.WriteLine("Shutting down the server...");
        }
    }
}
