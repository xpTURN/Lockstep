using System.Collections.Generic;
using xpTURN.Lockstep.Core;

namespace xpTURN.Lockstep.Input
{
    /// <summary>
    /// Input buffer interface
    /// Buffers input to compensate for network latency
    /// </summary>
    public interface IInputBuffer
    {
        /// <summary>
        /// Number of inputs stored in buffer
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Oldest tick number
        /// </summary>
        int OldestTick { get; }

        /// <summary>
        /// Newest tick number
        /// </summary>
        int NewestTick { get; }

        /// <summary>
        /// Add command
        /// </summary>
        void AddCommand(ICommand command);

        /// <summary>
        /// Get all commands for specific tick
        /// </summary>
        IEnumerable<ICommand> GetCommands(int tick);

        /// <summary>
        /// Get specific player's command for specific tick
        /// </summary>
        ICommand GetCommand(int tick, int playerId);

        /// <summary>
        /// Check if command exists for specific tick
        /// </summary>
        bool HasCommandForTick(int tick);

        /// <summary>
        /// Check if command exists for specific player and tick
        /// </summary>
        bool HasCommandForTick(int tick, int playerId);

        /// <summary>
        /// Remove all commands before specific tick
        /// </summary>
        void ClearBefore(int tick);

        /// <summary>
        /// Remove all commands after specific tick (used during rollback)
        /// </summary>
        void ClearAfter(int tick);

        /// <summary>
        /// Remove all commands
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// Input predictor interface
    /// Predicts other players' inputs that haven't been received yet
    /// </summary>
    public interface IInputPredictor
    {
        /// <summary>
        /// Predict input for specific player
        /// </summary>
        /// <param name="playerId">Player ID</param>
        /// <param name="tick">Tick to predict</param>
        /// <param name="previousCommands">Previous commands (used for prediction)</param>
        ICommand PredictInput(int playerId, int tick, IEnumerable<ICommand> previousCommands);

        /// <summary>
        /// Update prediction accuracy (compare with actual input)
        /// </summary>
        void UpdateAccuracy(ICommand predicted, ICommand actual);

        /// <summary>
        /// Prediction accuracy (0.0 ~ 1.0)
        /// </summary>
        float Accuracy { get; }
    }
}
