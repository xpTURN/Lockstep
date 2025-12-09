using System;

namespace xpTURN.Lockstep.Network
{
    /// <summary>
    /// Network transport layer interface
    /// Abstraction over actual network libraries (Photon, Mirror, etc.)
    /// </summary>
    public interface INetworkTransport
    {
        /// <summary>
        /// Connection state
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Local peer ID
        /// </summary>
        int LocalPeerId { get; }

        /// <summary>
        /// Connects to server
        /// </summary>
        void Connect(string address, int port);

        /// <summary>
        /// Disconnects
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Sends data to a specific peer
        /// </summary>
        void Send(int peerId, byte[] data, DeliveryMethod deliveryMethod);

        /// <summary>
        /// Broadcasts data to all peers
        /// </summary>
        void Broadcast(byte[] data, DeliveryMethod deliveryMethod);

        /// <summary>
        /// Processes received packets (called every frame)
        /// </summary>
        void PollEvents();

        /// <summary>
        /// Data received event
        /// </summary>
        event Action<int, byte[]> OnDataReceived; // peerId, data

        /// <summary>
        /// Peer connected event
        /// </summary>
        event Action<int> OnPeerConnected;

        /// <summary>
        /// Peer disconnected event
        /// </summary>
        event Action<int> OnPeerDisconnected;

        /// <summary>
        /// Connection successful event
        /// </summary>
        event Action OnConnected;

        /// <summary>
        /// Disconnected event
        /// </summary>
        event Action OnDisconnected;
    }

    /// <summary>
    /// Data delivery method
    /// </summary>
    public enum DeliveryMethod
    {
        /// <summary>
        /// Unreliable transmission (UDP)
        /// </summary>
        Unreliable,

        /// <summary>
        /// Reliable transmission
        /// </summary>
        Reliable,

        /// <summary>
        /// Reliable and ordered transmission
        /// </summary>
        ReliableOrdered,

        /// <summary>
        /// Ordered only
        /// </summary>
        Sequenced
    }
}
