using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Lockstep.Core;
using Lockstep.Core.Impl;

namespace Lockstep.Network.Impl
{
    /// <summary>
    /// Network message base class
    /// </summary>
    public abstract class NetworkMessageBase : INetworkMessage
    {
        public abstract NetworkMessageType MessageType { get; }

        public virtual byte[] Serialize()
        {
            // Use StreamPool to prevent GC
            using (var pooled = PooledMemoryStream.Create())
            {
                var ms = pooled.Stream;
                using (var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
                {
                    writer.Write((byte)MessageType);
                    SerializeData(writer);
                }
                return StreamPool.ToArrayExact(ms);
            }
        }

        public virtual void Deserialize(byte[] data, int offset, int length)
        {
            using (var ms = new MemoryStream(data, offset, length))
            using (var reader = new BinaryReader(ms))
            {
                // Assume MessageType was already read
                reader.ReadByte();
                DeserializeData(reader);
            }
        }

        protected abstract void SerializeData(BinaryWriter writer);
        protected abstract void DeserializeData(BinaryReader reader);
    }

    /// <summary>
    /// Command message
    /// </summary>
    public class CommandMessage : NetworkMessageBase
    {
        public override NetworkMessageType MessageType => NetworkMessageType.Command;

        public int Tick;
        public int PlayerId;
        public byte[] CommandData;

        protected override void SerializeData(BinaryWriter writer)
        {
            writer.Write(Tick);
            writer.Write(PlayerId);
            writer.Write(CommandData?.Length ?? 0);
            if (CommandData != null)
                writer.Write(CommandData);
        }

        protected override void DeserializeData(BinaryReader reader)
        {
            Tick = reader.ReadInt32();
            PlayerId = reader.ReadInt32();
            int length = reader.ReadInt32();
            if (length > 0)
                CommandData = reader.ReadBytes(length);
        }
    }

    /// <summary>
    /// Command acknowledgement message
    /// </summary>
    public class CommandAckMessage : NetworkMessageBase
    {
        public override NetworkMessageType MessageType => NetworkMessageType.CommandAck;

        public int Tick;
        public int PlayerId;

        protected override void SerializeData(BinaryWriter writer)
        {
            writer.Write(Tick);
            writer.Write(PlayerId);
        }

        protected override void DeserializeData(BinaryReader reader)
        {
            Tick = reader.ReadInt32();
            PlayerId = reader.ReadInt32();
        }
    }

    /// <summary>
    /// Sync hash message
    /// </summary>
    public class SyncHashMessage : NetworkMessageBase
    {
        public override NetworkMessageType MessageType => NetworkMessageType.SyncHash;

        public int Tick;
        public long Hash;
        public int PlayerId;

        protected override void SerializeData(BinaryWriter writer)
        {
            writer.Write(Tick);
            writer.Write(Hash);
            writer.Write(PlayerId);
        }

        protected override void DeserializeData(BinaryReader reader)
        {
            Tick = reader.ReadInt32();
            Hash = reader.ReadInt64();
            PlayerId = reader.ReadInt32();
        }
    }

    /// <summary>
    /// Game start message
    /// </summary>
    public class GameStartMessage : NetworkMessageBase
    {
        public override NetworkMessageType MessageType => NetworkMessageType.GameStart;

        public int RandomSeed;
        public int TickInterval;
        public int InputDelay;
        public List<int> PlayerIds = new List<int>();

        protected override void SerializeData(BinaryWriter writer)
        {
            writer.Write(RandomSeed);
            writer.Write(TickInterval);
            writer.Write(InputDelay);
            writer.Write(PlayerIds.Count);
            foreach (var id in PlayerIds)
                writer.Write(id);
        }

        protected override void DeserializeData(BinaryReader reader)
        {
            RandomSeed = reader.ReadInt32();
            TickInterval = reader.ReadInt32();
            InputDelay = reader.ReadInt32();
            int count = reader.ReadInt32();
            PlayerIds.Clear();
            for (int i = 0; i < count; i++)
                PlayerIds.Add(reader.ReadInt32());
        }
    }

    /// <summary>
    /// Player ready message
    /// </summary>
    public class PlayerReadyMessage : NetworkMessageBase
    {
        public override NetworkMessageType MessageType => NetworkMessageType.PlayerReady;

        public int PlayerId;
        public bool IsReady;

        protected override void SerializeData(BinaryWriter writer)
        {
            writer.Write(PlayerId);
            writer.Write(IsReady);
        }

        protected override void DeserializeData(BinaryReader reader)
        {
            PlayerId = reader.ReadInt32();
            IsReady = reader.ReadBoolean();
        }
    }

    /// <summary>
    /// Ping message
    /// </summary>
    public class PingMessage : NetworkMessageBase
    {
        public override NetworkMessageType MessageType => NetworkMessageType.Ping;

        public long Timestamp;
        public int Sequence;

        protected override void SerializeData(BinaryWriter writer)
        {
            writer.Write(Timestamp);
            writer.Write(Sequence);
        }

        protected override void DeserializeData(BinaryReader reader)
        {
            Timestamp = reader.ReadInt64();
            Sequence = reader.ReadInt32();
        }
    }

    /// <summary>
    /// Pong message
    /// </summary>
    public class PongMessage : NetworkMessageBase
    {
        public override NetworkMessageType MessageType => NetworkMessageType.Pong;

        public long Timestamp;
        public int Sequence;

        protected override void SerializeData(BinaryWriter writer)
        {
            writer.Write(Timestamp);
            writer.Write(Sequence);
        }

        protected override void DeserializeData(BinaryReader reader)
        {
            Timestamp = reader.ReadInt64();
            Sequence = reader.ReadInt32();
        }
    }

    /// <summary>
    /// Message serialization utility
    /// </summary>
    public class MessageSerializer : IMessageSerializer
    {
        private readonly Dictionary<NetworkMessageType, Func<INetworkMessage>> _creators
            = new Dictionary<NetworkMessageType, Func<INetworkMessage>>();

        public MessageSerializer()
        {
            // Register default message types
            RegisterMessageType<CommandMessage>(NetworkMessageType.Command);
            RegisterMessageType<CommandAckMessage>(NetworkMessageType.CommandAck);
            RegisterMessageType<SyncHashMessage>(NetworkMessageType.SyncHash);
            RegisterMessageType<GameStartMessage>(NetworkMessageType.GameStart);
            RegisterMessageType<PlayerReadyMessage>(NetworkMessageType.PlayerReady);
            RegisterMessageType<PingMessage>(NetworkMessageType.Ping);
            RegisterMessageType<PongMessage>(NetworkMessageType.Pong);
        }

        public void RegisterMessageType<T>(NetworkMessageType type) where T : INetworkMessage, new()
        {
            _creators[type] = () => new T();
        }

        public byte[] Serialize(INetworkMessage message)
        {
            return message.Serialize();
        }

        public INetworkMessage Deserialize(byte[] data)
        {
            if (data == null || data.Length < 1)
                return null;

            NetworkMessageType type = (NetworkMessageType)data[0];

            if (_creators.TryGetValue(type, out var creator))
            {
                var message = creator();
                message.Deserialize(data, 0, data.Length);
                return message;
            }

            return null;
        }
    }
}
