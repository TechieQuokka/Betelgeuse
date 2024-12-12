using AsynchronousServer;

namespace Betelgeuse
{
    public partial class MainProgram
    {
        private static void InitializePipeServer(PipeServer server)
        {
            server.Enter += Enter_EventCallback;
            server.EnterClientCommunication += EnterClientCommunication_EventCallback;
            return;
        }

        private static void EnterClientCommunication_EventCallback(object? sender, AsynchronousServer.DataType.ConnectedClient e)
        {
            return;
        }

        private static void Enter_EventCallback(object? sender, string name)
        {

            return;
        }
    }
}
