using System;
using Lockstep.Network;

namespace Lockstep.Core
{
    /// <summary>
    /// Lockstep engine state
    /// </summary>
    public enum LockstepState
    {
        Idle,
        WaitingForPlayers,
        Running,
        Paused,
        Finished
    }

    /// <summary>
    /// Lockstep main engine interface
    /// Manages network synchronization and simulation execution
    /// </summary>
    public interface ILockstepEngine
    {
        /// <summary>
        /// Current engine state
        /// </summary>
        LockstepState State { get; }

        /// <summary>
        /// Current tick number
        /// </summary>
        int CurrentTick { get; }

        /// <summary>
        /// Local player ID
        /// </summary>
        int LocalPlayerId { get; }

        /// <summary>
        /// Time per tick (milliseconds)
        /// </summary>
        int TickInterval { get; }

        /// <summary>
        /// Input delay tick count (network latency compensation)
        /// </summary>
        int InputDelay { get; }

        /// <summary>
        /// Initialize engine
        /// </summary>
        void Initialize(ISimulation simulation, ILockstepNetworkService networkService);

        /// <summary>
        /// Start game
        /// </summary>
        void Start();

        /// <summary>
        /// Update every frame
        /// </summary>
        void Update(float deltaTime);

        /// <summary>
        /// Input local command
        /// </summary>
        void InputCommand(ICommand command);

        /// <summary>
        /// Stop engine
        /// </summary>
        void Stop();

        /// <summary>
        /// Tick executed event
        /// </summary>
        event Action<int> OnTickExecuted;

        /// <summary>
        /// Desync detected event
        /// </summary>
        event Action<int, int> OnDesyncDetected; // localHash, remoteHash
    }

    /// <summary>
    /// Lockstep configuration interface
    /// </summary>
    public interface ILockstepConfig
    {
        /// <summary>
        /// Tick interval (milliseconds)
        /// </summary>
        int TickIntervalMs { get; }

        /// <summary>
        /// Input delay tick count
        /// </summary>
        int InputDelayTicks { get; }

        /// <summary>
        /// Maximum rollback tick count
        /// </summary>
        int MaxRollbackTicks { get; }

        /// <summary>
        /// Sync check interval (ticks)
        /// </summary>
        int SyncCheckInterval { get; }

        /// <summary>
        /// Whether to use prediction
        /// </summary>
        bool UsePrediction { get; }
    }
}
