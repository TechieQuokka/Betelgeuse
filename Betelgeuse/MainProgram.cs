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
            var pipeServer = new PipeServer("BetelgeuseLocalServer");
            InitializeLogin(pipeServer);

            var connection = _sqlConnection ?? throw new ArgumentNullException(nameof(_sqlConnection));

            try
            {
                connection.Open();
                Console.WriteLine("DB has been connected!");
                Console.WriteLine("The server has started!");
                var pipeServerTask = pipeServer.StartServerAsync();
                var waitForExitTask = WaitForExitKeyAsync(pipeServer);

                await Task.WhenAny(pipeServerTask, waitForExitTask);
            }
            finally
            {
                AES.Dispose();
                connection?.Close();
                connection?.Dispose();
                pipeServer?.Dispose();
            }
            return;
        }

        private static async Task WaitForExitKeyAsync(PipeServer server)
        {
            Console.WriteLine("Press the Delete key to shut down the server...");
            await Task.Run(() => { while (Console.ReadKey(true).Key != ConsoleKey.Delete) ; });

            server.Stop();
            Console.WriteLine("Shutting down the server...");
        }
    }
}
