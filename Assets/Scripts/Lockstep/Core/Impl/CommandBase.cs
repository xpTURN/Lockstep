using System;
using System.IO;
using System.Text;

namespace Lockstep.Core.Impl
{
    /// <summary>
    /// Command base class
    /// </summary>
    [Serializable]
    public abstract class CommandBase : ICommand
    {
        public int PlayerId { get; set; }
        public int Tick { get; set; }
        public abstract int CommandType { get; }

        protected CommandBase()
        {
        }

        protected CommandBase(int playerId, int tick)
        {
            PlayerId = playerId;
            Tick = tick;
        }

        public virtual byte[] Serialize()
        {
            // Use StreamPool to prevent GC
            using (var pooled = PooledMemoryStream.Create())
            {
                var ms = pooled.Stream;
                using (var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
                {
                    writer.Write(CommandType);
                    writer.Write(PlayerId);
                    writer.Write(Tick);
                    SerializeData(writer);
                }
                return StreamPool.ToArrayExact(ms);
            }
        }

        public virtual void Deserialize(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var reader = new BinaryReader(ms))
            {
                // Assume CommandType was already read (used by factory to determine type)
                int type = reader.ReadInt32();
                PlayerId = reader.ReadInt32();
                Tick = reader.ReadInt32();
                DeserializeData(reader);
            }
        }

        /// <summary>
        /// Serialize additional data in subclass
        /// </summary>
        protected abstract void SerializeData(BinaryWriter writer);

        /// <summary>
        /// Deserialize additional data in subclass
        /// </summary>
        protected abstract void DeserializeData(BinaryReader reader);
    }

    /// <summary>
    /// Empty command (no input)
    /// </summary>
    [Serializable]
    public class EmptyCommand : CommandBase
    {
        public const int TYPE_ID = 0;
        public override int CommandType => TYPE_ID;

        public EmptyCommand() : base() { }
        public EmptyCommand(int playerId, int tick) : base(playerId, tick) { }

        protected override void SerializeData(BinaryWriter writer) { }
        protected override void DeserializeData(BinaryReader reader) { }
    }

    /// <summary>
    /// Move command example
    /// </summary>
    [Serializable]
    public class MoveCommand : CommandBase
    {
        public const int TYPE_ID = 1;
        public override int CommandType => TYPE_ID;

        public long TargetX { get; set; }
        public long TargetY { get; set; }
        public long TargetZ { get; set; }

        public MoveCommand() : base() { }

        public MoveCommand(int playerId, int tick, long x, long y, long z) : base(playerId, tick)
        {
            TargetX = x;
            TargetY = y;
            TargetZ = z;
        }

        protected override void SerializeData(BinaryWriter writer)
        {
            writer.Write(TargetX);
            writer.Write(TargetY);
            writer.Write(TargetZ);
        }

        protected override void DeserializeData(BinaryReader reader)
        {
            TargetX = reader.ReadInt64();
            TargetY = reader.ReadInt64();
            TargetZ = reader.ReadInt64();
        }
    }

    /// <summary>
    /// Action command example (skill, attack, etc.)
    /// </summary>
    [Serializable]
    public class ActionCommand : CommandBase
    {
        public const int TYPE_ID = 2;
        public override int CommandType => TYPE_ID;

        public int ActionId { get; set; }
        public int TargetEntityId { get; set; }
        public long PositionX { get; set; }
        public long PositionY { get; set; }
        public long PositionZ { get; set; }

        public ActionCommand() : base() { }

        public ActionCommand(int playerId, int tick, int actionId, int targetEntityId = -1)
            : base(playerId, tick)
        {
            ActionId = actionId;
            TargetEntityId = targetEntityId;
        }

        protected override void SerializeData(BinaryWriter writer)
        {
            writer.Write(ActionId);
            writer.Write(TargetEntityId);
            writer.Write(PositionX);
            writer.Write(PositionY);
            writer.Write(PositionZ);
        }

        protected override void DeserializeData(BinaryReader reader)
        {
            ActionId = reader.ReadInt32();
            TargetEntityId = reader.ReadInt32();
            PositionX = reader.ReadInt64();
            PositionY = reader.ReadInt64();
            PositionZ = reader.ReadInt64();
        }
    }
}
