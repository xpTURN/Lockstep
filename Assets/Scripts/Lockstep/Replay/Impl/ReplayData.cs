using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lockstep.Core;
using Lockstep.Core.Impl;

namespace Lockstep.Replay.Impl
{
    /// <summary>
    /// Replay metadata implementation
    /// </summary>
    [Serializable]
    public class ReplayMetadata : IReplayMetadata
    {
        public const int CURRENT_VERSION = 1;
        
        public int Version { get; set; } = CURRENT_VERSION;
        public string SessionId { get; set; }
        public long RecordedAt { get; set; }
        public long DurationMs { get; set; }
        public int TotalTicks { get; set; }
        public int PlayerCount { get; set; }
        public int TickIntervalMs { get; set; }
        public int RandomSeed { get; set; }

        public ReplayMetadata()
        {
            SessionId = Guid.NewGuid().ToString("N");
            RecordedAt = DateTime.UtcNow.Ticks;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(SessionId ?? string.Empty);
            writer.Write(RecordedAt);
            writer.Write(DurationMs);
            writer.Write(TotalTicks);
            writer.Write(PlayerCount);
            writer.Write(TickIntervalMs);
            writer.Write(RandomSeed);
        }

        public void Deserialize(BinaryReader reader)
        {
            Version = reader.ReadInt32();
            SessionId = reader.ReadString();
            RecordedAt = reader.ReadInt64();
            DurationMs = reader.ReadInt64();
            TotalTicks = reader.ReadInt32();
            PlayerCount = reader.ReadInt32();
            TickIntervalMs = reader.ReadInt32();
            RandomSeed = reader.ReadInt32();
        }
    }

    /// <summary>
    /// Replay data implementation
    /// Contains all recorded commands organized by tick
    /// </summary>
    public class ReplayData : IReplayData
    {
        // Magic number for replay file identification
        private const uint MAGIC_NUMBER = 0x52504C59; // "RPLY"
        
        private readonly ReplayMetadata _metadata;
        private readonly Dictionary<int, List<ICommand>> _tickCommands;
        private readonly ICommandFactory _commandFactory;
        
        // Cached empty list for ticks with no commands
        private static readonly List<ICommand> EmptyCommandList = new List<ICommand>();

        public IReplayMetadata Metadata => _metadata;

        public ReplayData() : this(new CommandFactory())
        {
        }

        public ReplayData(ICommandFactory commandFactory)
        {
            _metadata = new ReplayMetadata();
            _tickCommands = new Dictionary<int, List<ICommand>>();
            _commandFactory = commandFactory;
        }

        /// <summary>
        /// Initialize replay data for recording
        /// </summary>
        public void Initialize(int playerCount, int tickIntervalMs, int randomSeed)
        {
            _metadata.PlayerCount = playerCount;
            _metadata.TickIntervalMs = tickIntervalMs;
            _metadata.RandomSeed = randomSeed;
            _metadata.RecordedAt = DateTime.UtcNow.Ticks;
            _tickCommands.Clear();
        }

        /// <summary>
        /// Add commands for a tick
        /// </summary>
        public void AddCommands(int tick, IEnumerable<ICommand> commands)
        {
            if (!_tickCommands.TryGetValue(tick, out var commandList))
            {
                commandList = ListPoolHelper.GetCommandList();
                _tickCommands[tick] = commandList;
            }

            foreach (var cmd in commands)
            {
                commandList.Add(cmd);
            }

            // Update metadata
            if (tick > _metadata.TotalTicks)
            {
                _metadata.TotalTicks = tick;
                _metadata.DurationMs = (long)tick * _metadata.TickIntervalMs;
            }
        }

        /// <summary>
        /// Finalize recording
        /// </summary>
        public void FinalizeRecording(int totalTicks)
        {
            _metadata.TotalTicks = totalTicks;
            _metadata.DurationMs = (long)_metadata.TotalTicks * _metadata.TickIntervalMs;
        }

        public IReadOnlyList<ICommand> GetCommandsForTick(int tick)
        {
            if (_tickCommands.TryGetValue(tick, out var commands))
            {
                return commands;
            }
            return EmptyCommandList;
        }

        public IReadOnlyDictionary<int, List<ICommand>> GetAllCommands()
        {
            return _tickCommands;
        }

        public byte[] Serialize()
        {
            using (var pooled = PooledMemoryStream.Create())
            {
                var ms = pooled.Stream;
                using (var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
                {
                    // Write magic number
                    writer.Write(MAGIC_NUMBER);
                    
                    // Write metadata
                    _metadata.Serialize(writer);
                    
                    // Write command data
                    writer.Write(_tickCommands.Count);
                    
                    foreach (var kvp in _tickCommands)
                    {
                        int tick = kvp.Key;
                        var commands = kvp.Value;
                        
                        writer.Write(tick);
                        writer.Write(commands.Count);
                        
                        foreach (var cmd in commands)
                        {
                            byte[] cmdData = cmd.Serialize();
                            writer.Write(cmdData.Length);
                            writer.Write(cmdData);
                        }
                    }
                }
                return StreamPool.ToArrayExact(ms);
            }
        }

        public void Deserialize(byte[] data)
        {
            if (data == null || data.Length < 4)
                throw new ArgumentException("Invalid replay data");

            _tickCommands.Clear();

            using (var ms = new MemoryStream(data))
            using (var reader = new BinaryReader(ms, Encoding.UTF8))
            {
                // Verify magic number
                uint magic = reader.ReadUInt32();
                if (magic != MAGIC_NUMBER)
                    throw new InvalidDataException("Invalid replay file format");
                
                // Read metadata
                _metadata.Deserialize(reader);
                
                // Verify version compatibility
                if (_metadata.Version > ReplayMetadata.CURRENT_VERSION)
                    throw new InvalidDataException($"Unsupported replay version: {_metadata.Version}");
                
                // Read command data
                int tickCount = reader.ReadInt32();
                
                for (int i = 0; i < tickCount; i++)
                {
                    int tick = reader.ReadInt32();
                    int commandCount = reader.ReadInt32();
                    
                    var commands = ListPoolHelper.GetCommandList();
                    _tickCommands[tick] = commands;
                    
                    for (int j = 0; j < commandCount; j++)
                    {
                        int cmdLength = reader.ReadInt32();
                        byte[] cmdData = reader.ReadBytes(cmdLength);
                        
                        ICommand cmd = _commandFactory.DeserializeCommand(cmdData);
                        if (cmd != null)
                        {
                            commands.Add(cmd);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Clear all data and return pooled lists
        /// </summary>
        public void Clear()
        {
            foreach (var list in _tickCommands.Values)
            {
                ListPoolHelper.ReturnCommandList(list);
            }
            _tickCommands.Clear();
        }
    }
}

