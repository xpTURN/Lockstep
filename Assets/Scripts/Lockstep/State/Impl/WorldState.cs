using System;
using System.IO;
using System.Collections.Generic;
using xpTURN.Lockstep.Core.Impl;

namespace xpTURN.Lockstep.State.Impl
{
    /// <summary>
    /// World state implementation
    /// </summary>
    public class WorldState : IWorldState
    {
        public int CurrentTick { get; private set; }

        private readonly Dictionary<int, IStateSyncable> _entities = new Dictionary<int, IStateSyncable>();
        private readonly List<IStateSyncable> _entityList = new List<IStateSyncable>();
        private IEntityFactory _entityFactory;

        private int _nextEntityId = 1;
        
        // Cached list for hash calculation (GC prevention)
        private readonly List<IStateSyncable> _sortedEntitiesCache = new List<IStateSyncable>();
        private static readonly Comparison<IStateSyncable> _entityIdComparison = (a, b) => a.EntityId.CompareTo(b.EntityId);
        
        // Cache for snapshot restoration (GC prevention)
        private readonly HashSet<int> _existingIdsCache = new HashSet<int>();
        private readonly List<int> _idsToRemoveCache = new List<int>();

        public IReadOnlyList<IStateSyncable> Entities => _entityList;
        public int EntityCount => _entityList.Count;

        public WorldState()
        {
        }

        public WorldState(IEntityFactory entityFactory)
        {
            _entityFactory = entityFactory;
        }

        public void SetEntityFactory(IEntityFactory factory)
        {
            _entityFactory = factory;
        }

        public void SetTick(int tick)
        {
            CurrentTick = tick;
        }

        public void AddEntity(IStateSyncable entity)
        {
            if (entity == null)
                return;

            int id = entity.EntityId;
            if (_entities.ContainsKey(id))
            {
                // Replace existing entity (manual loop instead of lambda for GC prevention)
                int index = -1;
                for (int i = 0; i < _entityList.Count; i++)
                {
                    if (_entityList[i].EntityId == id)
                    {
                        index = i;
                        break;
                    }
                }
                if (index >= 0)
                    _entityList[index] = entity;
            }
            else
            {
                _entityList.Add(entity);
            }

            _entities[id] = entity;

            // Update next ID
            if (id >= _nextEntityId)
                _nextEntityId = id + 1;
        }

        public void RemoveEntity(int entityId)
        {
            if (_entities.TryGetValue(entityId, out var entity))
            {
                _entities.Remove(entityId);
                _entityList.Remove(entity);
            }
        }

        public IStateSyncable GetEntity(int entityId)
        {
            _entities.TryGetValue(entityId, out var entity);
            return entity;
        }

        public bool HasEntity(int entityId)
        {
            return _entities.ContainsKey(entityId);
        }

        /// <summary>
        /// Get list of entities of specific type (manual loop instead of LINQ for GC prevention)
        /// Note: Do not modify the returned list
        /// </summary>
        public void GetEntitiesOfType<T>(List<T> result) where T : class, IStateSyncable
        {
            result.Clear();
            for (int i = 0; i < _entityList.Count; i++)
            {
                if (_entityList[i] is T typed)
                {
                    result.Add(typed);
                }
            }
        }

        public int GenerateEntityId()
        {
            return _nextEntityId++;
        }

        public IStateSnapshot CreateSnapshot()
        {
            // Use StreamPool to prevent GC
            using (var pooled = PooledMemoryStream.Create())
            {
                var ms = pooled.Stream;
                using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
                {
                    writer.Write(CurrentTick);
                    writer.Write(_nextEntityId);
                    writer.Write(_entityList.Count);

                    foreach (var entity in _entityList)
                    {
                        writer.Write(entity.EntityId);
                        writer.Write(entity.EntityTypeId);

                        byte[] entityData = entity.SerializeState();
                        writer.Write(entityData.Length);
                        writer.Write(entityData);
                    }
                }

                var snapshot = new StateSnapshot(CurrentTick);
                snapshot.SetData(StreamPool.ToArrayExact(ms));
                return snapshot;
            }
        }

        public void RestoreFromSnapshot(IStateSnapshot snapshot)
        {
            if (snapshot == null)
                return;

            var stateSnapshot = snapshot as StateSnapshot;
            if (stateSnapshot == null)
                return;

            byte[] data = stateSnapshot.GetData();
            if (data == null || data.Length == 0)
                return;

            using (var ms = new MemoryStream(data))
            using (var reader = new BinaryReader(ms))
            {
                CurrentTick = reader.ReadInt32();
                _nextEntityId = reader.ReadInt32();
                int entityCount = reader.ReadInt32();

                // Find entities not in snapshot (use cache for GC prevention)
                _existingIdsCache.Clear();
                foreach (var key in _entities.Keys)
                {
                    _existingIdsCache.Add(key);
                }

                for (int i = 0; i < entityCount; i++)
                {
                    int entityId = reader.ReadInt32();
                    int entityTypeId = reader.ReadInt32();
                    int dataLength = reader.ReadInt32();
                    byte[] entityData = reader.ReadBytes(dataLength);

                    _existingIdsCache.Remove(entityId);

                    if (_entities.TryGetValue(entityId, out var entity))
                    {
                        // Restore existing entity state
                        entity.DeserializeState(entityData);
                    }
                    else if (_entityFactory != null)
                    {
                        // Create new entity
                        entity = _entityFactory.CreateEntity(entityTypeId);
                        if (entity != null)
                        {
                            entity.DeserializeState(entityData);
                            AddEntity(entity);
                        }
                    }
                }

                // Remove entities not in snapshot (use cache for GC prevention)
                _idsToRemoveCache.Clear();
                foreach (int id in _existingIdsCache)
                {
                    _idsToRemoveCache.Add(id);
                }
                for (int i = 0; i < _idsToRemoveCache.Count; i++)
                {
                    RemoveEntity(_idsToRemoveCache[i]);
                }
            }
        }

        public long CalculateHash()
        {
            // Sort by ID for deterministic order (use cached list instead of LINQ for GC prevention)
            _sortedEntitiesCache.Clear();
            for (int i = 0; i < _entityList.Count; i++)
            {
                _sortedEntitiesCache.Add(_entityList[i]);
            }
            _sortedEntitiesCache.Sort(_entityIdComparison);

            ulong hash = 14695981039346656037UL;

            hash ^= (ulong)CurrentTick;
            hash *= 1099511628211UL;

            hash ^= (ulong)_entityList.Count;
            hash *= 1099511628211UL;

            foreach (var entity in _sortedEntitiesCache)
            {
                ulong entityHash = entity.GetStateHash();
                hash ^= (ulong)entityHash;
                hash *= 1099511628211UL;
            }

            return (long)hash;
        }

        public void Clear()
        {
            _entities.Clear();
            _entityList.Clear();
            CurrentTick = 0;
            _nextEntityId = 1;
        }
    }
}
