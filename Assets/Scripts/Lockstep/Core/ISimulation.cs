using System.Collections.Generic;

namespace xpTURN.Lockstep.Core
{
    /// <summary>
    /// Deterministic simulation interface
    /// Must guarantee same results for same inputs
    /// </summary>
    public interface ISimulation
    {
        /// <summary>
        /// Current simulation tick
        /// </summary>
        int CurrentTick { get; }

        /// <summary>
        /// Initialize simulation
        /// </summary>
        void Initialize();

        /// <summary>
        /// Execute single tick simulation
        /// </summary>
        /// <param name="commands">Commands to execute this tick</param>
        void Tick(IEnumerable<ICommand> commands);

        /// <summary>
        /// Rollback to specific tick (used when prediction fails)
        /// </summary>
        void Rollback(int targetTick);

        /// <summary>
        /// Return hash of current state (for sync verification)
        /// </summary>
        long GetStateHash();

        /// <summary>
        /// Reset simulation
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// Simulatable game entity interface
    /// </summary>
    public interface ISimulatable
    {
        /// <summary>
        /// Entity unique ID
        /// </summary>
        int EntityId { get; }

        /// <summary>
        /// Perform deterministic update
        /// </summary>
        /// <param name="deltaTime">Fixed delta time (milliseconds)</param>
        void SimulationUpdate(int deltaTime);

        /// <summary>
        /// Apply command
        /// </summary>
        void ApplyCommand(ICommand command);
    }
}
