using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Lockstep.Core;
using Lockstep.Core.Impl;
using Lockstep.Math.Impl;

namespace Lockstep.State.Impl
{
    /// <summary>
    /// Syncable entity base class
    /// </summary>
    [Serializable]
    public abstract class EntityBase : IStateSyncable, Core.ISimulatable
    {
        public int EntityId { get; protected set; }
        public abstract int EntityTypeId { get; }

        // Basic transform info (fixed point)
        public FPVector3 Position;
        public FP64 Rotation; // Y-axis rotation (degrees)
        public FPVector3 Scale;

        // Owner info
        public int OwnerId { get; set; }

        protected EntityBase()
        {
            Scale = FPVector3.One;
        }

        protected EntityBase(int entityId) : this()
        {
            EntityId = entityId;
        }

        public virtual void SimulationUpdate(int deltaTimeMs)
        {
            // Implement in subclass
        }

        public virtual void ApplyCommand(ICommand command)
        {
            // Implement in subclass
        }

        public virtual byte[] SerializeState()
        {
            // Use StreamPool to prevent GC
            using (var pooled = PooledMemoryStream.Create())
            {
                var ms = pooled.Stream;
                using (var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
                {
                    writer.Write(EntityId);
                    writer.Write(OwnerId);

                    // Transform info
                    writer.Write(Position.x.RawValue);
                    writer.Write(Position.y.RawValue);
                    writer.Write(Position.z.RawValue);
                    writer.Write(Rotation.RawValue);
                    writer.Write(Scale.x.RawValue);
                    writer.Write(Scale.y.RawValue);
                    writer.Write(Scale.z.RawValue);

                    // Subclass data
                    SerializeCustomState(writer);
                }
                return StreamPool.ToArrayExact(ms);
            }
        }

        public virtual void DeserializeState(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var reader = new BinaryReader(ms))
            {
                EntityId = reader.ReadInt32();
                OwnerId = reader.ReadInt32();

                // Transform info
                Position = new FPVector3(
                    FP64.FromRaw(reader.ReadInt64()),
                    FP64.FromRaw(reader.ReadInt64()),
                    FP64.FromRaw(reader.ReadInt64())
                );
                Rotation = FP64.FromRaw(reader.ReadInt64());
                Scale = new FPVector3(
                    FP64.FromRaw(reader.ReadInt64()),
                    FP64.FromRaw(reader.ReadInt64()),
                    FP64.FromRaw(reader.ReadInt64())
                );

                // Subclass data
                DeserializeCustomState(reader);
            }
        }

        public virtual ulong GetStateHash()
        {
            ulong hash = 14695981039346656037UL;

            hash ^= (ulong)EntityId;
            hash *= 1099511628211UL;

            hash ^= (ulong)Position.x.RawValue;
            hash *= 1099511628211UL;

            hash ^= (ulong)Position.y.RawValue;
            hash *= 1099511628211UL;

            hash ^= (ulong)Position.z.RawValue;
            hash *= 1099511628211UL;

            hash ^= (ulong)Rotation.RawValue;
            hash *= 1099511628211UL;

            hash = GetCustomStateHash(hash);

            return (ulong)hash;
        }

        public virtual void ResetState()
        {
            Position = FPVector3.Zero;
            Rotation = FP64.Zero;
            Scale = FPVector3.One;
            OwnerId = 0;
        }

        /// <summary>
        /// Serialize additional state in subclass
        /// </summary>
        protected virtual void SerializeCustomState(BinaryWriter writer) { }

        /// <summary>
        /// Deserialize additional state in subclass
        /// </summary>
        protected virtual void DeserializeCustomState(BinaryReader reader) { }

        /// <summary>
        /// Additional state hash in subclass
        /// </summary>
        protected virtual ulong GetCustomStateHash(ulong currentHash)
        {
            return (ulong)currentHash;
        }
    }

    /// <summary>
    /// Unit entity example (movement, HP, etc.)
    /// </summary>
    [Serializable]
    public class UnitEntity : EntityBase
    {
        public const int TYPE_ID = 1;
        public override int EntityTypeId => TYPE_ID;

        public FP64 MoveSpeed;
        public int MaxHealth;
        public int CurrentHealth;
        public FPVector3 Velocity;
        public FPVector3 TargetPosition;
        public bool IsMoving;

        public UnitEntity() : base()
        {
            MoveSpeed = FP64.FromInt(5);
            MaxHealth = 100;
            CurrentHealth = 100;
        }

        public UnitEntity(int entityId) : base(entityId)
        {
            MoveSpeed = FP64.FromInt(5);
            MaxHealth = 100;
            CurrentHealth = 100;
        }

        public override void SimulationUpdate(int deltaTimeMs)
        {
            if (!IsMoving)
                return;

            // Move towards target
            FPVector3 direction = TargetPosition - Position;
            FP64 distance = direction.magnitude;

            if (distance < FP64.FromFloat(0.1f))
            {
                Position = TargetPosition;
                IsMoving = false;
                Velocity = FPVector3.Zero;
                return;
            }

            FPVector3 normalizedDir = direction.normalized;
            FP64 deltaSeconds = FP64.FromInt(deltaTimeMs) / FP64.FromInt(1000);
            FP64 moveDistance = MoveSpeed * deltaSeconds;

            if (moveDistance > distance)
                moveDistance = distance;

            Velocity = normalizedDir * MoveSpeed;
            Position = Position + normalizedDir * moveDistance;
        }

        public override void ApplyCommand(ICommand command)
        {
            if (command.PlayerId != OwnerId)
                return;

            if (command is Core.Impl.MoveCommand moveCmd)
            {
                TargetPosition = new FPVector3(
                    FP64.FromRaw(moveCmd.TargetX),
                    FP64.FromRaw(moveCmd.TargetY),
                    FP64.FromRaw(moveCmd.TargetZ)
                );
                IsMoving = true;
            }
        }

        public void TakeDamage(int damage)
        {
            CurrentHealth -= damage;
            if (CurrentHealth <= 0)
            {
                CurrentHealth = 0;
                // Handle death (remove from simulation)
            }
        }

        protected override void SerializeCustomState(BinaryWriter writer)
        {
            writer.Write(MoveSpeed.RawValue);
            writer.Write(MaxHealth);
            writer.Write(CurrentHealth);
            writer.Write(Velocity.x.RawValue);
            writer.Write(Velocity.y.RawValue);
            writer.Write(Velocity.z.RawValue);
            writer.Write(TargetPosition.x.RawValue);
            writer.Write(TargetPosition.y.RawValue);
            writer.Write(TargetPosition.z.RawValue);
            writer.Write(IsMoving);
        }

        protected override void DeserializeCustomState(BinaryReader reader)
        {
            MoveSpeed = FP64.FromRaw(reader.ReadInt64());
            MaxHealth = reader.ReadInt32();
            CurrentHealth = reader.ReadInt32();
            Velocity = new FPVector3(
                FP64.FromRaw(reader.ReadInt64()),
                FP64.FromRaw(reader.ReadInt64()),
                FP64.FromRaw(reader.ReadInt64())
            );
            TargetPosition = new FPVector3(
                FP64.FromRaw(reader.ReadInt64()),
                FP64.FromRaw(reader.ReadInt64()),
                FP64.FromRaw(reader.ReadInt64())
            );
            IsMoving = reader.ReadBoolean();
        }

        protected override ulong GetCustomStateHash(ulong currentHash)
        {
            ulong hash = (ulong)currentHash;

            hash ^= (ulong)CurrentHealth;
            hash *= 1099511628211UL;

            hash ^= (ulong)Velocity.x.RawValue;
            hash *= 1099511628211UL;

            hash ^= (ulong)IsMoving.GetHashCode();
            hash *= 1099511628211UL;

            return (ulong)hash;
        }

        public override void ResetState()
        {
            base.ResetState();
            CurrentHealth = MaxHealth;
            Velocity = FPVector3.Zero;
            TargetPosition = FPVector3.Zero;
            IsMoving = false;
        }
    }

    /// <summary>
    /// Entity factory implementation
    /// </summary>
    public class EntityFactory : IEntityFactory
    {
        private readonly Dictionary<int, Func<IStateSyncable>> _creators
            = new Dictionary<int, Func<IStateSyncable>>();

        public EntityFactory()
        {
            // Register default entity types
            RegisterEntityType<UnitEntity>(UnitEntity.TYPE_ID);
        }

        public void RegisterEntityType<T>(int entityTypeId) where T : IStateSyncable, new()
        {
            _creators[entityTypeId] = () => new T();
        }

        public IStateSyncable CreateEntity(int entityTypeId)
        {
            if (_creators.TryGetValue(entityTypeId, out var creator))
            {
                return creator();
            }

            throw new ArgumentException($"Unknown entity type: {entityTypeId}");
        }

        public void DestroyEntity(IStateSyncable entity)
        {
            // Handle pooling if needed
            entity?.ResetState();
        }
    }
}
