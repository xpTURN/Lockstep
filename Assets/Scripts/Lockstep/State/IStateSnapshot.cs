using System.Collections.Generic;

namespace Lockstep.State
{
    /// <summary>
    /// Game state snapshot interface
    /// Used for state save/restore for rollback
    /// </summary>
    public interface IStateSnapshot
    {
        /// <summary>
        /// Snapshot tick number
        /// </summary>
        int Tick { get; }

        /// <summary>
        /// Serialize snapshot data to byte array
        /// </summary>
        byte[] Serialize();

        /// <summary>
        /// Restore snapshot from byte array
        /// </summary>
        void Deserialize(byte[] data);

        /// <summary>
        /// Calculate state hash
        /// </summary>
        ulong CalculateHash();
    }

    /// <summary>
    /// State snapshot manager interface
    /// </summary>
    public interface IStateSnapshotManager
    {
        /// <summary>
        /// Maximum number of snapshots to store
        /// </summary>
        int MaxSnapshots { get; set; }

        /// <summary>
        /// Save state snapshot for current tick
        /// </summary>
        void SaveSnapshot(int tick, IStateSnapshot snapshot);

        /// <summary>
        /// Get snapshot for specific tick
        /// </summary>
        IStateSnapshot GetSnapshot(int tick);

        /// <summary>
        /// Check if snapshot exists for specific tick
        /// </summary>
        bool HasSnapshot(int tick);

        /// <summary>
        /// Delete all snapshots after specific tick (used during rollback)
        /// </summary>
        void ClearSnapshotsAfter(int tick);

        /// <summary>
        /// Delete all snapshots
        /// </summary>
        void ClearAll();

        /// <summary>
        /// List of saved snapshot ticks
        /// </summary>
        IEnumerable<int> SavedTicks { get; }
    }
}
