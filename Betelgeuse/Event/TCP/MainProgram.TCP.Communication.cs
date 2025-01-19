using System.Text.Json;
using AsynchronousServer;
using Standard.DataType;

namespace Betelgeuse
{
    internal partial class MainProgram
    {
        internal static void InitializeTCPCommunication (IServer server)
        {
            server.ReceiveData += ReceiveData_EventCallback;
            return;
        }

        private static void ReceiveData_EventCallback(object sender, AsynchronousServer.DataType.ClientCommunicationEventArgs argument)
        {
            var header = JsonSerializer.Deserialize<Header>(argument.Data);

            Console.WriteLine(header?.Data);
            return;
        }
    }
}
