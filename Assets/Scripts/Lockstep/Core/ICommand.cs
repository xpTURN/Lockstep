namespace Lockstep.Core
{
    /// <summary>
    /// Command interface representing player input
    /// All game commands must implement this interface
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Player ID who issued this command
        /// </summary>
        int PlayerId { get; }

        /// <summary>
        /// Tick number when this command should be executed
        /// </summary>
        int Tick { get; }

        /// <summary>
        /// Command type identifier
        /// </summary>
        int CommandType { get; }

        /// <summary>
        /// Serialize command to byte array
        /// </summary>
        byte[] Serialize();

        /// <summary>
        /// Deserialize command from byte array
        /// </summary>
        void Deserialize(byte[] data);
    }

    /// <summary>
    /// Command factory interface
    /// </summary>
    public interface ICommandFactory
    {
        /// <summary>
        /// Create appropriate command instance based on command type
        /// </summary>
        ICommand CreateCommand(int commandType);

        /// <summary>
        /// Restore command from byte data
        /// </summary>
        ICommand DeserializeCommand(byte[] data);
    }
}
