using System;
using xpTURN.Lockstep.Core;

namespace xpTURN.Lockstep.Input
{
    /// <summary>
    /// Input handler interface
    /// Collects player input and converts to commands
    /// </summary>
    public interface IInputHandler
    {
        /// <summary>
        /// Local player ID
        /// </summary>
        int LocalPlayerId { get; set; }

        /// <summary>
        /// Input capture enabled
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Start input capture
        /// </summary>
        void StartCapture();

        /// <summary>
        /// Stop input capture
        /// </summary>
        void StopCapture();

        /// <summary>
        /// Capture input every frame (called from Unity Update)
        /// </summary>
        void CaptureInput();

        /// <summary>
        /// Create command for current tick
        /// </summary>
        ICommand CreateCommand(int tick);

        /// <summary>
        /// Clear input buffer
        /// </summary>
        void ClearBuffer();

        /// <summary>
        /// Command created event
        /// </summary>
        event Action<ICommand> OnCommandCreated;
    }

    /// <summary>
    /// Input state interface (deterministic input data)
    /// </summary>
    public interface IInputState
    {
        /// <summary>
        /// Tick number
        /// </summary>
        int Tick { get; }

        /// <summary>
        /// Player ID
        /// </summary>
        int PlayerId { get; }

        /// <summary>
        /// Whether input is empty (no input)
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Clone input state
        /// </summary>
        IInputState Clone();

        /// <summary>
        /// Compare input states
        /// </summary>
        bool Equals(IInputState other);
    }
}
