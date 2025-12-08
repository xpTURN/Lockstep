using System;
using System.IO;
using System.Collections.Generic;
using Lockstep.Core.Impl;

namespace Lockstep.State.Impl
{
    /// <summary>
    /// State snapshot implementation
    /// </summary>
    [Serializable]
    public class StateSnapshot : IStateSnapshot
    {
        public int Tick { get; private set; }

        private byte[] _data;
        private ulong _hash;
        private bool _hashCalculated;

        public StateSnapshot(int tick)
        {
            Tick = tick;
        }

        public StateSnapshot(int tick, byte[] data) : this(tick)
        {
            _data = data;
        }

        public byte[] Serialize()
        {
            // Use StreamPool to prevent GC
            using (var pooled = PooledMemoryStream.Create())
            {
                var ms = pooled.Stream;
                using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
                {
                    writer.Write(Tick);
                    writer.Write(_data?.Length ?? 0);
                    if (_data != null)
                        writer.Write(_data);
                }
                return StreamPool.ToArrayExact(ms);
            }
        }

        public void Deserialize(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var reader = new BinaryReader(ms))
            {
                Tick = reader.ReadInt32();
                int length = reader.ReadInt32();
                if (length > 0)
                    _data = reader.ReadBytes(length);

                _hashCalculated = false;
            }
        }

        public ulong CalculateHash()
        {
            if (_hashCalculated)
                return _hash;

            if (_data == null || _data.Length == 0)
            {
                _hash = 0;
                _hashCalculated = true;
                return _hash;
            }

            // FNV-1a hash
            _hash = 14695981039346656037UL;
            foreach (byte b in _data)
            {
                _hash ^= b;
                _hash *= 1099511628211UL;
            }

            _hashCalculated = true;
            return (ulong)_hash;
        }

        public byte[] GetData()
        {
            return _data;
        }

        public void SetData(byte[] data)
        {
            _data = data;
            _hashCalculated = false;
        }
    }

    /// <summary>
    /// State snapshot manager implementation
    /// </summary>
    public class StateSnapshotManager : IStateSnapshotManager
    {
        private readonly Dictionary<int, IStateSnapshot> _snapshots = new Dictionary<int, IStateSnapshot>();
        private readonly LinkedList<int> _tickOrder = new LinkedList<int>();
        
        // Cached list (GC prevention)
        private readonly List<int> _ticksToRemoveCache = new List<int>();

        public int MaxSnapshots { get; set; } = 60; // Default 1 second worth (60fps)

        public IEnumerable<int> SavedTicks => _tickOrder;

        public void SaveSnapshot(int tick, IStateSnapshot snapshot)
        {
            // Overwrite existing snapshot
            if (_snapshots.ContainsKey(tick))
            {
                _snapshots[tick] = snapshot;
                return;
            }

            // Add new snapshot
            _snapshots[tick] = snapshot;
            _tickOrder.AddLast(tick);

            // Remove oldest when exceeding max count
            while (_tickOrder.Count > MaxSnapshots)
            {
                int oldestTick = _tickOrder.First.Value;
                _tickOrder.RemoveFirst();
                _snapshots.Remove(oldestTick);
            }
        }

        public IStateSnapshot GetSnapshot(int tick)
        {
            if (_snapshots.TryGetValue(tick, out var snapshot))
                return snapshot;
            return null;
        }

        public bool HasSnapshot(int tick)
        {
            return _snapshots.ContainsKey(tick);
        }

        public void ClearSnapshotsAfter(int tick)
        {
            // Use cached list for GC prevention
            _ticksToRemoveCache.Clear();

            foreach (var t in _snapshots.Keys)
            {
                if (t > tick)
                    _ticksToRemoveCache.Add(t);
            }

            for (int i = 0; i < _ticksToRemoveCache.Count; i++)
            {
                int t = _ticksToRemoveCache[i];
                _snapshots.Remove(t);
                _tickOrder.Remove(t);
            }
        }

        public void ClearAll()
        {
            _snapshots.Clear();
            _tickOrder.Clear();
        }

        /// <summary>
        /// Find nearest snapshot before specific tick
        /// </summary>
        public IStateSnapshot GetNearestSnapshot(int tick)
        {
            int nearest = -1;
            foreach (var t in _snapshots.Keys)
            {
                if (t <= tick && t > nearest)
                    nearest = t;
            }

            if (nearest >= 0)
                return _snapshots[nearest];

            return null;
        }
    }
}
