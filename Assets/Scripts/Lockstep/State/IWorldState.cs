using System.Collections.Generic;

namespace Lockstep.State
{
    /// <summary>
    /// World state interface
    /// Manages all entity states
    /// </summary>
    public interface IWorldState
    {
        /// <summary>
        /// Current tick number
        /// </summary>
        int CurrentTick { get; }

        /// <summary>
        /// All entities list
        /// </summary>
        IReadOnlyList<IStateSyncable> Entities { get; }

        /// <summary>
        /// Entity count
        /// </summary>
        int EntityCount { get; }

        /// <summary>
        /// Add entity
        /// </summary>
        void AddEntity(IStateSyncable entity);

        /// <summary>
        /// Remove entity
        /// </summary>
        void RemoveEntity(int entityId);

        /// <summary>
        /// Get entity
        /// </summary>
        IStateSyncable GetEntity(int entityId);

        /// <summary>
        /// Check if entity exists
        /// </summary>
        bool HasEntity(int entityId);

        /// <summary>
        /// Get all entities of specific type
        /// </summary>
        public void GetEntitiesOfType<T>(List<T> result) where T : class, IStateSyncable;

        /// <summary>
        /// Create full state snapshot
        /// </summary>
        IStateSnapshot CreateSnapshot();

        /// <summary>
        /// Restore state from snapshot
        /// </summary>
        void RestoreFromSnapshot(IStateSnapshot snapshot);

        /// <summary>
        /// Calculate full state hash
        /// </summary>
        long CalculateHash();

        /// <summary>
        /// Clear state
        /// </summary>
        void Clear();
    }
}
