using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace xpTURN.Lockstep.Core.Impl
{
    /// <summary>
    /// Command factory implementation
    /// </summary>
    public class CommandFactory : ICommandFactory
    {
        private readonly Dictionary<int, Func<ICommand>> _creators = new Dictionary<int, Func<ICommand>>();
        
        // Cached list (GC prevention)
        private readonly List<ICommand> _commandListCache = new List<ICommand>();

        public CommandFactory()
        {
            // Register default command types
            RegisterCommand<EmptyCommand>(EmptyCommand.TYPE_ID);
            RegisterCommand<MoveCommand>(MoveCommand.TYPE_ID);
            RegisterCommand<ActionCommand>(ActionCommand.TYPE_ID);
        }

        /// <summary>
        /// Register command type
        /// </summary>
        public void RegisterCommand<T>(int commandType) where T : ICommand, new()
        {
            _creators[commandType] = () => new T();
        }

        public ICommand CreateCommand(int commandType)
        {
            if (_creators.TryGetValue(commandType, out var creator))
            {
                return creator();
            }

            throw new ArgumentException($"Unknown command type: {commandType}");
        }

        public ICommand DeserializeCommand(byte[] data)
        {
            if (data == null || data.Length < 4)
                return null;

            // Read command type from first 4 bytes
            int commandType = BitConverter.ToInt32(data, 0);

            ICommand command = CreateCommand(commandType);
            command.Deserialize(data);

            return command;
        }

        /// <summary>
        /// Serialize command array (uses StreamPool to prevent GC)
        /// </summary>
        public byte[] SerializeCommands(IEnumerable<ICommand> commands)
        {
            using (var pooled = PooledMemoryStream.Create())
            {
                var ms = pooled.Stream;
                using (var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
                {
                    // Use cached list to count first
                    _commandListCache.Clear();
                    foreach (var cmd in commands)
                    {
                        _commandListCache.Add(cmd);
                    }
                    
                    writer.Write(_commandListCache.Count);

                    for (int i = 0; i < _commandListCache.Count; i++)
                    {
                        byte[] cmdData = _commandListCache[i].Serialize();
                        writer.Write(cmdData.Length);
                        writer.Write(cmdData);
                    }
                }
                return StreamPool.ToArrayExact(ms);
            }
        }

        /// <summary>
        /// Deserialize command array (GC-Free version)
        /// Note: Returned list contents will change on next call
        /// </summary>
        public List<ICommand> DeserializeCommands(byte[] data)
        {
            _commandListCache.Clear();

            using (var ms = new MemoryStream(data))
            using (var reader = new BinaryReader(ms))
            {
                int count = reader.ReadInt32();

                for (int i = 0; i < count; i++)
                {
                    int length = reader.ReadInt32();
                    byte[] cmdData = reader.ReadBytes(length);
                    ICommand cmd = DeserializeCommand(cmdData);
                    if (cmd != null)
                        _commandListCache.Add(cmd);
                }
            }

            return _commandListCache;
        }
    }
}
