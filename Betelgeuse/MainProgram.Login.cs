using AsynchronousServer;

namespace Betelgeuse
{
    public partial class MainProgram
    {
        private static void InitializeLogin(PipeServer server)
        {
            server.EnterClientCommunication += Login_EventHandler;
            return;
        }

        private static void Login_EventHandler(object? sender, AsynchronousServer.DataType.ConnectedClient client)
        {
            var pipeServer = sender as PipeServer ?? throw new ArgumentNullException(nameof(sender));
            var connection = _sqlConnection;


            return;
        }
    }
}
