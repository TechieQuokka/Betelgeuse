using System.Diagnostics;
using AsynchronousServer;
using log4net;
using Standard.DataType;
using Standard.Static;

namespace Betelgeuse.Extension
{
    public static class Network
    {
        public static void SendDataWithLogging (this Stream stream, IServer server, Guid clientId, string message, StackTrace stackTrace, Status status = Status.None, bool sendToClient = true)
        {
            if (stream is null) throw new ArgumentNullException(nameof(stream));
            if (stackTrace is null) throw new ArgumentNullException(nameof(stackTrace));

            ILog log = MainProgram.Log;
            var data = System.Text.Encoding.UTF8.GetBytes(Common.ToJson(message, status.ToString()));
            if (sendToClient) server.SendInChunks(stream, data);

            var logMessage = string.Format("{0} {1} - {2}:{3}", DateTime.Now.ToString("g"), clientId, status, message);
            Console.WriteLine(logMessage);
            switch (status)
            {
                case Status.Error:
                    log.ErrorFormat("{0}\n{1}", logMessage, stackTrace.ToString());
                    break;
                case Status.Fatal:
                    log.FatalFormat("{0}\n{1}", logMessage, stackTrace.ToString());
                    break;
                case Status.Warning:
                    log.WarnFormat("{0}\n{1}", logMessage, stackTrace.ToString());
                    break;
                case Status.Information:
                    log.InfoFormat("{0}", logMessage);
                    break;
                default:
                    log.DebugFormat("{0}", logMessage);
                    break;
            }
            return;
        }
    }
}
