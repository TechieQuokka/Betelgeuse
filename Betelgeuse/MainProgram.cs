using AsynchronousServer;
using Betelgeuse.Global;
using MySql.Data.MySqlClient;

namespace Betelgeuse
{
    public partial class MainProgram
    {
        private static MySqlConnection? _sqlConnection;
        public static async Task Main(string[] args)
        {
            var pipeServer = new PipeServer("BetelgeuseLocalServer");
            InitializeLogin(pipeServer);
            InitializePipeServer(pipeServer);

            var connection = _sqlConnection = new MySqlConnection(DatabaseVariable.connectionString);

            try
            {
                connection.Open();
                Console.WriteLine("DB has been connected!");
                var pipeServerTask = pipeServer.StartServerAsync();

                await Task.WhenAny(pipeServerTask);
            }
            finally
            {
                pipeServer.EnterClientCommunication -= Login_EventHandler;
                connection?.Close();
                connection?.Dispose();
                pipeServer?.Dispose();
            }
            return;
        }
    }
}
