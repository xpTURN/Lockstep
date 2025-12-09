using System;
using System.Collections.Generic;
using xpTURN.Lockstep.Core;
using xpTURN.Lockstep.Input.Impl;

namespace xpTURN.Lockstep.Network.Impl
{
    /// <summary>
    /// Player info implementation
    /// </summary>
    public class PlayerInfo : IPlayerInfo
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; }
        public bool IsReady { get; set; }
        public int Ping { get; set; }
    }

    /// <summary>
    /// Lockstep network service implementation
    /// </summary>
    public class LockstepNetworkService : ILockstepNetworkService
    {
        private INetworkTransport _transport;
        private ICommandFactory _commandFactory;
        private MessageSerializer _messageSerializer;
        private InputBuffer _inputBuffer;

        private readonly List<PlayerInfo> _players = new List<PlayerInfo>();
        private readonly Dictionary<int, int> _peerToPlayer = new Dictionary<int, int>();
        private readonly Dictionary<int, long> _syncHashes = new Dictionary<int, long>();
        
        // Cached list (GC prevention)
        private readonly List<int> _hashKeysToRemoveCache = new List<int>();

        public int PlayerCount => _players.Count;
        public bool AllPlayersReady => _players.TrueForAll(p => p.IsReady);
        public int LocalPlayerId { get; private set; }
        public bool IsHost { get; private set; }
        public IReadOnlyList<IPlayerInfo> Players => _players;

        public event Action OnGameStart;
        public event Action<IPlayerInfo> OnPlayerJoined;
        public event Action<IPlayerInfo> OnPlayerLeft;
        public event Action<ICommand> OnCommandReceived;
        public event Action<int, int, long, long> OnDesyncDetected;

        public void Initialize(INetworkTransport transport, ICommandFactory commandFactory)
        {
            _transport = transport;
            _commandFactory = commandFactory;
            _messageSerializer = new MessageSerializer();
            _inputBuffer = new InputBuffer();

            // Connect network events
            _transport.OnDataReceived += HandleDataReceived;
            _transport.OnPeerConnected += HandlePeerConnected;
            _transport.OnPeerDisconnected += HandlePeerDisconnected;
            _transport.OnConnected += HandleConnected;
        }

        public void CreateRoom(string roomName, int maxPlayers)
        {
            IsHost = true;
            LocalPlayerId = 0;

            // Add host as player
            var hostPlayer = new PlayerInfo
            {
                PlayerId = LocalPlayerId,
                PlayerName = "Host",
                IsReady = false
            };
            _players.Add(hostPlayer);
        }

        public void JoinRoom(string roomName)
        {
            IsHost = false;
            // Player ID assigned by server
        }

        public void LeaveRoom()
        {
            _players.Clear();
            _peerToPlayer.Clear();
            _inputBuffer.Clear();
        }

        public void SetReady(bool ready)
        {
            var localPlayer = _players.Find(p => p.PlayerId == LocalPlayerId);
            if (localPlayer != null)
            {
                localPlayer.IsReady = ready;

                // Broadcast ready state
                var msg = new PlayerReadyMessage
                {
                    PlayerId = LocalPlayerId,
                    IsReady = ready
                };
                BroadcastMessage(msg);
            }

            // Host: start game when all players are ready
            if (IsHost && AllPlayersReady && _players.Count >= 1)
            {
                StartGame();
            }
        }

        private void StartGame()
        {
            var msg = new GameStartMessage
            {
                RandomSeed = Environment.TickCount,
                TickInterval = 50, // 20 ticks per second
                InputDelay = 2
            };

            foreach (var player in _players)
            {
                msg.PlayerIds.Add(player.PlayerId);
            }

            BroadcastMessage(msg);
            OnGameStart?.Invoke();
        }

        public void SendCommand(ICommand command)
        {
            // Save to local buffer
            _inputBuffer.AddCommand(command);

            // Send over network
            var msg = new CommandMessage
            {
                Tick = command.Tick,
                PlayerId = command.PlayerId,
                CommandData = command.Serialize()
            };

            BroadcastMessage(msg);
        }

        public void RequestCommandsForTick(int tick)
        {
            // Implement retransmission request if needed
        }

        public bool HasCommandsForTick(int tick)
        {
            return _inputBuffer.HasAllCommands(tick, _players.Count);
        }

        public IEnumerable<ICommand> GetCommandsForTick(int tick)
        {
            return _inputBuffer.GetCommands(tick);
        }

        public void SendSyncHash(int tick, long hash)
        {
            var msg = new SyncHashMessage
            {
                Tick = tick,
                Hash = hash,
                PlayerId = LocalPlayerId
            };

            BroadcastMessage(msg);
        }

        public void Update()
        {
            _transport?.PollEvents();
        }

        private void HandleDataReceived(int peerId, byte[] data)
        {
            var message = _messageSerializer.Deserialize(data);
            if (message == null)
                return;

            switch (message)
            {
                case CommandMessage cmdMsg:
                    HandleCommandMessage(cmdMsg);
                    break;

                case SyncHashMessage hashMsg:
                    HandleSyncHashMessage(hashMsg);
                    break;

                case GameStartMessage startMsg:
                    HandleGameStartMessage(startMsg);
                    break;

                case PlayerReadyMessage readyMsg:
                    HandlePlayerReadyMessage(readyMsg);
                    break;

                case PingMessage pingMsg:
                    HandlePingMessage(peerId, pingMsg);
                    break;

                case PongMessage pongMsg:
                    HandlePongMessage(peerId, pongMsg);
                    break;
            }
        }

        private void HandleCommandMessage(CommandMessage msg)
        {
            var command = _commandFactory.DeserializeCommand(msg.CommandData);
            if (command != null)
            {
                _inputBuffer.AddCommand(command);
                OnCommandReceived?.Invoke(command);
            }
        }

        private void HandleSyncHashMessage(SyncHashMessage msg)
        {
            // Store and compare hash
            int key = msg.Tick * 1000 + msg.PlayerId;
            _syncHashes[key] = msg.Hash;

            // Compare with local hash
            int localKey = msg.Tick * 1000 + LocalPlayerId;
            if (_syncHashes.TryGetValue(localKey, out long localHash))
            {
                if (localHash != msg.Hash)
                {
                    OnDesyncDetected?.Invoke(msg.PlayerId, msg.Tick, localHash, msg.Hash);
                }
            }
        }

        private void HandleGameStartMessage(GameStartMessage msg)
        {
            // Update player list
            _players.Clear();
            for (int i = 0; i < msg.PlayerIds.Count; i++)
            {
                var player = new PlayerInfo
                {
                    PlayerId = msg.PlayerIds[i],
                    IsReady = true
                };
                _players.Add(player);
            }

            OnGameStart?.Invoke();
        }

        private void HandlePlayerReadyMessage(PlayerReadyMessage msg)
        {
            var player = _players.Find(p => p.PlayerId == msg.PlayerId);
            if (player != null)
            {
                player.IsReady = msg.IsReady;
            }
        }

        private void HandlePingMessage(int peerId, PingMessage msg)
        {
            // Pong response
            var pong = new PongMessage
            {
                Timestamp = msg.Timestamp,
                Sequence = msg.Sequence
            };
            _transport.Send(peerId, pong.Serialize(), DeliveryMethod.Unreliable);
        }

        private void HandlePongMessage(int peerId, PongMessage msg)
        {
            // Calculate RTT
            long rtt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - msg.Timestamp;

            if (_peerToPlayer.TryGetValue(peerId, out int playerId))
            {
                var player = _players.Find(p => p.PlayerId == playerId);
                if (player != null)
                {
                    player.Ping = (int)rtt;
                }
            }
        }

        private void HandlePeerConnected(int peerId)
        {
            if (IsHost)
            {
                // Assign new player ID
                int newPlayerId = _players.Count;
                _peerToPlayer[peerId] = newPlayerId;

                var newPlayer = new PlayerInfo
                {
                    PlayerId = newPlayerId,
                    PlayerName = $"Player{newPlayerId}",
                    IsReady = false
                };
                _players.Add(newPlayer);

                OnPlayerJoined?.Invoke(newPlayer);
            }
        }

        private void HandlePeerDisconnected(int peerId)
        {
            if (_peerToPlayer.TryGetValue(peerId, out int playerId))
            {
                var player = _players.Find(p => p.PlayerId == playerId);
                if (player != null)
                {
                    _players.Remove(player);
                    OnPlayerLeft?.Invoke(player);
                }
                _peerToPlayer.Remove(peerId);
            }
        }

        private void HandleConnected()
        {
            // Client connected to server
        }

        private void BroadcastMessage(INetworkMessage message)
        {
            byte[] data = message.Serialize();
            _transport?.Broadcast(data, DeliveryMethod.ReliableOrdered);
        }

        /// <summary>
        /// Clear data before specific tick
        /// </summary>
        public void ClearOldData(int tick)
        {
            _inputBuffer.ClearBefore(tick);

            // Remove old hashes (use cached list for GC prevention)
            _hashKeysToRemoveCache.Clear();
            foreach (var key in _syncHashes.Keys)
            {
                int hashTick = key / 1000;
                if (hashTick < tick)
                    _hashKeysToRemoveCache.Add(key);
            }
            for (int i = 0; i < _hashKeysToRemoveCache.Count; i++)
            {
                _syncHashes.Remove(_hashKeysToRemoveCache[i]);
            }
        }
    }
}
