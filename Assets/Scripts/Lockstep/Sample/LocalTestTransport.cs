using System;
using System.Collections.Generic;
using Lockstep.Network;

namespace Lockstep.Sample
{
    /// <summary>
    /// Local test network transport
    /// Allows testing Lockstep system without actual network
    /// </summary>
    public class LocalTestTransport : INetworkTransport
    {
        private static LocalTestTransport _hostInstance;
        private static List<LocalTestTransport> _clients = new List<LocalTestTransport>();

        private Queue<(int peerId, byte[] data)> _incomingMessages = new Queue<(int, byte[])>();
        private bool _isHost;
        private bool _isConnected;
        private int _peerId;

        public bool IsConnected => _isConnected;
        public int LocalPeerId => _peerId;
        public bool IsHost => _isHost;

        // INetworkTransport events
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<int, byte[]> OnDataReceived;
        public event Action<int> OnPeerConnected;
        public event Action<int> OnPeerDisconnected;

        public void Connect(string address, int port)
        {
            _isHost = false;
            _peerId = _clients.Count + 1; // Start from 1 (0 is host)
            _clients.Add(this);
            _isConnected = true;

            // Notify host of connection
            _hostInstance?.NotifyPeerConnected(_peerId);

            OnConnected?.Invoke();
        }

        /// <summary>
        /// Start as host (test extension method)
        /// </summary>
        public void Host(int port, int maxConnections)
        {
            _isHost = true;
            _peerId = 0;
            _hostInstance = this;
            _isConnected = true;
            OnConnected?.Invoke();
        }

        public void Send(int peerId, byte[] data, DeliveryMethod deliveryMethod)
        {
            // In local test, DeliveryMethod is ignored (always reliable)
            if (peerId == 0 && _hostInstance != null)
            {
                _hostInstance.ReceiveMessage(_peerId, data);
            }
            else if (peerId > 0 && peerId <= _clients.Count)
            {
                _clients[peerId - 1].ReceiveMessage(_peerId, data);
            }
        }

        public void Broadcast(byte[] data, DeliveryMethod deliveryMethod)
        {
            if (_isHost)
            {
                // Host → All clients
                foreach (var client in _clients)
                {
                    client.ReceiveMessage(_peerId, data);
                }
            }
            else
            {
                // Client → Host → All clients (relay)
                _hostInstance?.ReceiveAndBroadcast(_peerId, data);
            }
        }

        public void PollEvents()
        {
            while (_incomingMessages.Count > 0)
            {
                var (peerId, data) = _incomingMessages.Dequeue();
                OnDataReceived?.Invoke(peerId, data);
            }
        }

        public void Disconnect()
        {
            _isConnected = false;
            if (_isHost)
            {
                // Notify all clients of disconnection
                foreach (var client in _clients)
                {
                    client.OnPeerDisconnected?.Invoke(0);
                }
                _hostInstance = null;
                _clients.Clear();
            }
            else
            {
                _clients.Remove(this);
                // Notify host of disconnection
                _hostInstance?.NotifyPeerDisconnected(_peerId);
            }
            OnDisconnected?.Invoke();
        }

        private void ReceiveMessage(int fromPeerId, byte[] data)
        {
            _incomingMessages.Enqueue((fromPeerId, data));
        }

        private void ReceiveAndBroadcast(int fromPeerId, byte[] data)
        {
            // Host also receives
            _incomingMessages.Enqueue((fromPeerId, data));

            // Send to other clients (excluding sender)
            foreach (var client in _clients)
            {
                if (client._peerId != fromPeerId)
                {
                    client.ReceiveMessage(fromPeerId, data);
                }
            }
        }

        private void NotifyPeerConnected(int peerId)
        {
            OnPeerConnected?.Invoke(peerId);
        }

        private void NotifyPeerDisconnected(int peerId)
        {
            OnPeerDisconnected?.Invoke(peerId);
        }

        /// <summary>
        /// Reset all instances (for testing)
        /// </summary>
        public static void Reset()
        {
            _hostInstance = null;
            _clients.Clear();
        }

        /// <summary>
        /// Number of connected peers (host only)
        /// </summary>
        public int PeerCount => _isHost ? _clients.Count : 0;
    }
}
