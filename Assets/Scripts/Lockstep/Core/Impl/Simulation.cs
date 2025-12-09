using System;
using System.Collections.Generic;
using xpTURN.Lockstep.State;
using xpTURN.Lockstep.State.Impl;
using xpTURN.Lockstep.Math.Impl;

namespace xpTURN.Lockstep.Core.Impl
{
    /// <summary>
    /// Simulation implementation
    /// </summary>
    public class Simulation : ISimulation
    {
        public int CurrentTick => _worldState.CurrentTick;

        private readonly WorldState _worldState;
        private readonly StateSnapshotManager _snapshotManager;
        private readonly DeterministicRandom _random;

        private int _tickIntervalMs;
        private readonly List<ISimulatable> _simulatables = new List<ISimulatable>();

        public event Action<int> OnTick;
        public event Action<int> OnRollback;

        public Simulation()
        {
            _worldState = new WorldState(new EntityFactory());
            _snapshotManager = new StateSnapshotManager();
            _random = new DeterministicRandom();
            _tickIntervalMs = 50; // Default 20 ticks/second
        }

        public Simulation(int tickIntervalMs) : this()
        {
            _tickIntervalMs = tickIntervalMs;
        }

        public void Initialize()
        {
            _worldState.Clear();
            _snapshotManager.ClearAll();
            _random.SetSeed(0);
        }

        public void Initialize(int randomSeed)
        {
            Initialize();
            _random.SetSeed(randomSeed);
        }

        public void Tick(IEnumerable<ICommand> commands)
        {
            // Apply commands
            foreach (var command in commands)
            {
                ApplyCommand(command);
            }

            // Update simulation
            UpdateSimulation();

            // Increment tick
            _worldState.SetTick(CurrentTick + 1);

            OnTick?.Invoke(CurrentTick);
        }

        private void ApplyCommand(ICommand command)
        {
            // Apply command to the player's entities
            foreach (var entity in _worldState.Entities)
            {
                if (entity is EntityBase entityBase && entityBase.OwnerId == command.PlayerId)
                {
                    if (entity is ISimulatable simulatable)
                    {
                        simulatable.ApplyCommand(command);
                    }
                }
            }
        }

        private void UpdateSimulation()
        {
            // Update all entities
            foreach (var entity in _worldState.Entities)
            {
                if (entity is ISimulatable simulatable)
                {
                    simulatable.SimulationUpdate(_tickIntervalMs);
                }
            }

            // Additional system updates (collision, AI, etc.)
            UpdateSystems();
        }

        protected virtual void UpdateSystems()
        {
            // Implement collision detection, AI, etc. in subclasses
        }

        public void Rollback(int targetTick)
        {
            var snapshot = _snapshotManager.GetSnapshot(targetTick);
            if (snapshot == null)
            {
                // Find nearest snapshot
                snapshot = _snapshotManager.GetNearestSnapshot(targetTick);
            }

            if (snapshot != null)
            {
                _worldState.RestoreFromSnapshot(snapshot);
                _snapshotManager.ClearSnapshotsAfter(targetTick);
                OnRollback?.Invoke(targetTick);
            }
        }

        public long GetStateHash()
        {
            return _worldState.CalculateHash();
        }

        public void Reset()
        {
            _worldState.Clear();
            _snapshotManager.ClearAll();
        }

        /// <summary>
        /// Create snapshot
        /// </summary>
        public IStateSnapshot CreateSnapshot(int tick)
        {
            var snapshot = _worldState.CreateSnapshot();
            _snapshotManager.SaveSnapshot(tick, snapshot);
            return snapshot;
        }

        /// <summary>
        /// Add entity
        /// </summary>
        public void AddEntity(IStateSyncable entity)
        {
            _worldState.AddEntity(entity);

            if (entity is ISimulatable simulatable)
            {
                _simulatables.Add(simulatable);
            }
        }

        /// <summary>
        /// Remove entity
        /// </summary>
        public void RemoveEntity(int entityId)
        {
            var entity = _worldState.GetEntity(entityId);
            if (entity != null)
            {
                _worldState.RemoveEntity(entityId);

                if (entity is ISimulatable simulatable)
                {
                    _simulatables.Remove(simulatable);
                }
            }
        }

        /// <summary>
        /// Get entity
        /// </summary>
        public IStateSyncable GetEntity(int entityId)
        {
            return _worldState.GetEntity(entityId);
        }

        /// <summary>
        /// All entities
        /// </summary>
        public IReadOnlyList<IStateSyncable> Entities => _worldState.Entities;

        /// <summary>
        /// Deterministic random number generator
        /// </summary>
        public DeterministicRandom Random => _random;

        /// <summary>
        /// World state
        /// </summary>
        public WorldState WorldState => _worldState;

        /// <summary>
        /// Generate new entity ID
        /// </summary>
        public int GenerateEntityId()
        {
            return _worldState.GenerateEntityId();
        }

        /// <summary>
        /// Spawn entity for specific player
        /// </summary>
        public T SpawnEntity<T>(int ownerId) where T : EntityBase, new()
        {
            var entity = new T();
            var entityBase = entity as EntityBase;
            if (entityBase != null)
            {
                // Need reflection or protected setter to set EntityBase's EntityId
                // Here we set ID through DeserializeState
            }
            entity.OwnerId = ownerId;
            AddEntity(entity);
            return entity;
        }
    }
}
