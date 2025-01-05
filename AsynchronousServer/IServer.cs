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
        /// Gets a dictionary of clients currently connected to the server, 
        /// with each client identified by a unique Guid.
        /// </summary>
        IDictionary<Guid, ConnectedClient> ConnectedClients { get; }

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
        /// Disconnects a specific client from the server based on the client ID.
        /// </summary>
        /// <param name="clientId">The unique identifier of the client to disconnect.</param>
        /// <returns>A boolean value indicating whether the client was successfully disconnected.</returns>
        bool Disconnect(Guid clientId);

        /// <summary>
        /// This method interrupts the task associated with the given clientId and waits for it to complete.
        /// </summary>
        /// <param name="clientId">The unique identifier of the client whose task needs to be interrupted.</param>
        /// <returns>Returns true if the task was successfully interrupted, otherwise false.</returns>
        bool Kill(Guid clientId);

        /// <summary>
        /// Stops the server and cancels all ongoing operations.
        /// </summary>
        void Stop();

        /// <summary>
        /// Sends data in chunks through the stream.
        /// </summary>
        /// <param name="data">The data to be sent in chunks.</param>
        void SendInChunks(in Stream stream, byte[] data);

        /// <summary>
        /// Receives data in chunks from the stream.
        /// </summary>
        /// <returns>A byte array containing the received data.</returns>
        byte[] ReceiveInChunks(in Stream stream);
    }
}
