using System.IO;
using Lockstep.Core;
using Lockstep.Core.Impl;
using Lockstep.State.Impl;
using Lockstep.Math.Impl;

namespace Lockstep.Sample
{
    /// <summary>
    /// Unit controlled by the player
    /// </summary>
    public class PlayerUnit : EntityBase
    {
        public const int TYPE_ID = 100;
        public override int EntityTypeId => TYPE_ID;

        // Movement related
        public FP64 MoveSpeed = FP64.FromInt(5);
        public FPVector3 Velocity;
        public FPVector3 TargetPosition;
        public bool IsMoving;

        // Health
        public int MaxHealth = 100;
        public int CurrentHealth;

        // Attack
        public int AttackDamage = 10;
        public int AttackCooldownMs = 1000;
        private int _attackCooldownRemaining;

#if UNITY_2021_1_OR_NEWER
        // Linked GameObject (for rendering)
        [System.NonSerialized] public UnityEngine.GameObject GameObject;
#endif

        public PlayerUnit() : base()
        {
            CurrentHealth = MaxHealth;
        }

        public override void SimulationUpdate(int deltaTimeMs)
        {
            // Update attack cooldown
            if (_attackCooldownRemaining > 0)
            {
                _attackCooldownRemaining -= deltaTimeMs;
            }

            // Process movement
            if (IsMoving)
            {
                UpdateMovement(deltaTimeMs);
            }
        }

        private void UpdateMovement(int deltaTimeMs)
        {
            FPVector3 direction = TargetPosition - Position;
            FP64 distance = direction.magnitude;

            // Target reached
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

            // Update rotation (Y axis)
            Rotation = FP64.Atan2(normalizedDir.x, normalizedDir.z) * FP64.Rad2Deg;
        }

        public override void ApplyCommand(ICommand command)
        {
            // Only process own commands
            if (command.PlayerId != OwnerId)
                return;

            switch (command)
            {
                case MoveCommand moveCmd:
                    HandleMoveCommand(moveCmd);
                    break;

                case ActionCommand actionCmd:
                    HandleActionCommand(actionCmd);
                    break;
            }
        }

        private void HandleMoveCommand(MoveCommand cmd)
        {
            TargetPosition = new FPVector3(
                FP64.FromRaw(cmd.TargetX),
                FP64.FromRaw(cmd.TargetY),
                FP64.FromRaw(cmd.TargetZ)
            );
            IsMoving = true;
        }

        private void HandleActionCommand(ActionCommand cmd)
        {
            switch (cmd.ActionId)
            {
                case 1: // Attack
                    TryAttack(cmd.TargetEntityId);
                    break;

                case 2: // Move command (right click)
                    TargetPosition = new FPVector3(
                        FP64.FromRaw(cmd.PositionX),
                        FP64.FromRaw(cmd.PositionY),
                        FP64.FromRaw(cmd.PositionZ)
                    );
                    IsMoving = true;
                    break;
            }
        }

        private void TryAttack(int targetEntityId)
        {
            if (_attackCooldownRemaining > 0)
                return;

            // Actual attack logic is processed in simulation
            _attackCooldownRemaining = AttackCooldownMs;
        }

        public void TakeDamage(int damage)
        {
            CurrentHealth -= damage;
            if (CurrentHealth <= 0)
            {
                CurrentHealth = 0;
                // Death handling (remove from simulation)
            }
        }

        protected override void SerializeCustomState(BinaryWriter writer)
        {
            writer.Write(MoveSpeed.RawValue);
            writer.Write(CurrentHealth);
            writer.Write(MaxHealth);
            writer.Write(Velocity.x.RawValue);
            writer.Write(Velocity.y.RawValue);
            writer.Write(Velocity.z.RawValue);
            writer.Write(TargetPosition.x.RawValue);
            writer.Write(TargetPosition.y.RawValue);
            writer.Write(TargetPosition.z.RawValue);
            writer.Write(IsMoving);
            writer.Write(_attackCooldownRemaining);
        }

        protected override void DeserializeCustomState(BinaryReader reader)
        {
            MoveSpeed = FP64.FromRaw(reader.ReadInt64());
            CurrentHealth = reader.ReadInt32();
            MaxHealth = reader.ReadInt32();
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
            _attackCooldownRemaining = reader.ReadInt32();
        }

        protected override ulong GetCustomStateHash(ulong currentHash)
        {
            ulong hash = currentHash;

            hash ^= (ulong)CurrentHealth;
            hash *= 1099511628211UL;

            hash ^= (ulong)Velocity.x.RawValue;
            hash *= 1099511628211UL;

            hash ^= (ulong)IsMoving.GetHashCode();
            hash *= 1099511628211UL;

            return hash;
        }
    }
}
