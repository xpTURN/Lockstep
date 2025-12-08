namespace Lockstep.Network
{
    /// <summary>
    /// Network message type
    /// </summary>
    public enum NetworkMessageType : byte
    {
        // Lobby related
        JoinRoom = 1,
        LeaveRoom = 2,
        PlayerReady = 3,
        GameStart = 4,

        // Gameplay related
        Command = 10,
        CommandAck = 11,
        CommandRequest = 12,

        // Synchronization related
        SyncHash = 20,
        SyncHashAck = 21,
        FullState = 22,

        // Connection related
        Ping = 30,
        Pong = 31,
        Disconnect = 32
    }

    /// <summary>
    /// Network message interface
    /// </summary>
    public interface INetworkMessage
    {
        /// <summary>
        /// Message type
        /// </summary>
        NetworkMessageType MessageType { get; }

        /// <summary>
        /// Serializes the message into a byte array
        /// </summary>
        byte[] Serialize();

        /// <summary>
        /// Deserializes the message from a byte array
        /// </summary>
        void Deserialize(byte[] data, int offset, int length);
    }

    /// <summary>
    /// Message serialization utility interface
    /// </summary>
    public interface IMessageSerializer
    {
        /// <summary>
        /// Serializes a message
        /// </summary>
        byte[] Serialize(INetworkMessage message);

        /// <summary>
        /// Deserializes a message
        /// </summary>
        INetworkMessage Deserialize(byte[] data);

        /// <summary>
        /// Registers a message type
        /// </summary>
        void RegisterMessageType<T>(NetworkMessageType type) where T : INetworkMessage, new();
    }
}
