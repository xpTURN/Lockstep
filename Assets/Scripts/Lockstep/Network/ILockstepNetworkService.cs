using System;
using System.Collections.Generic;
using xpTURN.Lockstep.Core;

namespace xpTURN.Lockstep.Network
{
    /// <summary>
    /// Player information
    /// </summary>
    public interface IPlayerInfo
    {
        int PlayerId { get; }
        string PlayerName { get; }
        bool IsReady { get; }
        int Ping { get; }
    }

    /// <summary>
    /// Lockstep network service interface
    /// Responsible for command synchronization and player management
    /// </summary>
    public interface ILockstepNetworkService
    {
        /// <summary>
        /// Number of connected players
        /// </summary>
        int PlayerCount { get; }

        /// <summary>
        /// Whether all players are ready
        /// </summary>
        bool AllPlayersReady { get; }

        /// <summary>
        /// Local player ID
        /// </summary>
        int LocalPlayerId { get; }

        /// <summary>
        /// Whether this is the host
        /// </summary>
        bool IsHost { get; }

        /// <summary>
        /// Information of all connected players
        /// </summary>
        IReadOnlyList<IPlayerInfo> Players { get; }

        /// <summary>
        /// Initializes the network
        /// </summary>
        void Initialize(INetworkTransport transport, ICommandFactory commandFactory);

        /// <summary>
        /// Creates a room (host)
        /// </summary>
        void CreateRoom(string roomName, int maxPlayers);

        /// <summary>
        /// Joins a room
        /// </summary>
        void JoinRoom(string roomName);

        /// <summary>
        /// Leaves the room
        /// </summary>
        void LeaveRoom();

        /// <summary>
        /// Sets ready state
        /// </summary>
        void SetReady(bool ready);

        /// <summary>
        /// Sends a command (sends own input to other players)
        /// </summary>
        void SendCommand(ICommand command);

        /// <summary>
        /// Waits for commands for a specific tick
        /// </summary>
        void RequestCommandsForTick(int tick);

        /// <summary>
        /// Whether all commands for a specific tick have been received
        /// </summary>
        bool HasCommandsForTick(int tick);

        /// <summary>
        /// Gets commands for a specific tick
        /// </summary>
        IEnumerable<ICommand> GetCommandsForTick(int tick);

        /// <summary>
        /// Sends synchronization hash
        /// </summary>
        void SendSyncHash(int tick, long hash);

        /// <summary>
        /// Updates every frame
        /// </summary>
        void Update();

        void ClearOldData(int tick);

        /// <summary>
        /// Game start event
        /// </summary>
        event Action OnGameStart;

        /// <summary>
        /// Player joined event
        /// </summary>
        event Action<IPlayerInfo> OnPlayerJoined;

        /// <summary>
        /// Player left event
        /// </summary>
        event Action<IPlayerInfo> OnPlayerLeft;

        /// <summary>
        /// Command received event
        /// </summary>
        event Action<ICommand> OnCommandReceived;

        /// <summary>
        /// Desync detected event
        /// </summary>
        event Action<int, int, long, long> OnDesyncDetected; // playerId, tick, localHash, remoteHash
    }
}
