using AsynchronousServer.DataType;

namespace AsynchronousServer
{
    /// <summary>
    /// Defines the basic functionality and events for a TCP server. 
    /// This interface includes properties for obtaining server state information, 
    /// and events for handling client connections and data communication. 
    /// It also provides methods to start and stop the server.
    /// </summary>
    public interface IServer : IDisposable
    {
        /// <summary>
        /// Gets the number of clients currently connected to the server.
        /// </summary>
        int ConnectedClientCount { get; }
        /// <summary>
        /// Gets the size of the data chunks used for communication.
        /// </summary>
        int ChunkSize { get; }

        /// <summary>
        /// Occurs when the server is started.
        /// </summary>
        event EventHandler? Enter;

        /// <summary>
        /// Occurs when a client successfully connects to the server.
        /// </summary>
        event StartServerEventHandler? Connected;

        /// <summary>
        /// Occurs when the server enters client communication mode.
        /// </summary>
        event EventHandler<ConnectedClient>? EnterClientCommunication;

        /// <summary>
        /// Occurs when data is received from a client.
        /// </summary>
        event ClientCommunicationEventHandler? ReceiveData;

        /// <summary>
        /// Occurs when the server is stopped.
        /// </summary>
        event EventHandler? StopServer;


        /// <summary>
        /// Starts the server asynchronously, allowing it to accept client connections and handle data communication.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task StartServerAsync();

        /// <summary>
        /// Stops the server and cancels all ongoing operations.
        /// </summary>
        void Stop();
    }
}