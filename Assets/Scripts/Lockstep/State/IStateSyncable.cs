namespace xpTURN.Lockstep.State
{
    /// <summary>
    /// State syncable object interface
    /// All game objects that need to be synced must implement this
    /// </summary>
    public interface IStateSyncable
    {
        /// <summary>
        /// Unique entity ID
        /// </summary>
        int EntityId { get; }

        /// <summary>
        /// Entity type ID (used by factory)
        /// </summary>
        int EntityTypeId { get; }

        /// <summary>
        /// Serialize current state to byte array
        /// </summary>
        byte[] SerializeState();

        /// <summary>
        /// Restore state from byte array
        /// </summary>
        void DeserializeState(byte[] data);

        /// <summary>
        /// Calculate state hash (for sync verification)
        /// </summary>
        ulong GetStateHash();

        /// <summary>
        /// Reset state
        /// </summary>
        void ResetState();
    }

    /// <summary>
    /// State syncable entity factory
    /// </summary>
    public interface IEntityFactory
    {
        /// <summary>
        /// Create entity by entity type ID
        /// </summary>
        IStateSyncable CreateEntity(int entityTypeId);

        /// <summary>
        /// Destroy entity
        /// </summary>
        void DestroyEntity(IStateSyncable entity);

        /// <summary>
        /// Register entity type
        /// </summary>
        void RegisterEntityType<T>(int entityTypeId) where T : IStateSyncable, new();
    }
}
