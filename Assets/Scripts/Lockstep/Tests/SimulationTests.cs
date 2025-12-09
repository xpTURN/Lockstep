using NUnit.Framework;
using System.Collections.Generic;
using xpTURN.Lockstep.Core;
using xpTURN.Lockstep.Core.Impl;
using xpTURN.Lockstep.State.Impl;
using xpTURN.Lockstep.Math.Impl;

namespace xpTURN.Lockstep.Tests
{
    /// <summary>
    /// Simulation tests
    /// </summary>
    [TestFixture]
    public class SimulationTests
    {
        private Simulation _simulation;

        [SetUp]
        public void SetUp()
        {
            _simulation = new Simulation(50); // 50ms per tick
            _simulation.Initialize();
        }

        #region Initialization

        [Test]
        public void Initialize_SetsTickToZero()
        {
            Assert.AreEqual(0, _simulation.CurrentTick);
        }

        [Test]
        public void Initialize_ClearsEntities()
        {
            // Add entity then reinitialize
            var entity = new UnitEntity();
            _simulation.AddEntity(entity);

            _simulation.Initialize();

            Assert.AreEqual(0, _simulation.Entities.Count);
        }

        #endregion

        #region Tick Processing

        [Test]
        public void Tick_IncrementsTick()
        {
            _simulation.Tick(new List<ICommand>());
            Assert.AreEqual(1, _simulation.CurrentTick);

            _simulation.Tick(new List<ICommand>());
            Assert.AreEqual(2, _simulation.CurrentTick);
        }

        [Test]
        public void Tick_MultipleTicks_IncrementsCorrectly()
        {
            for (int i = 0; i < 100; i++)
            {
                _simulation.Tick(new List<ICommand>());
            }

            Assert.AreEqual(100, _simulation.CurrentTick);
        }

        #endregion

        #region Entity Management

        [Test]
        public void AddEntity_IncreasesCount()
        {
            Assert.AreEqual(0, _simulation.Entities.Count);

            var entity = new UnitEntity();
            _simulation.AddEntity(entity);

            Assert.AreEqual(1, _simulation.Entities.Count);
        }

        [Test]
        public void SpawnEntity_CreatesEntityWithOwner()
        {
            var entity = _simulation.SpawnEntity<UnitEntity>(5);

            Assert.IsNotNull(entity);
            Assert.AreEqual(5, entity.OwnerId);
        }

        [Test]
        public void GetEntity_ReturnsCorrectEntity()
        {
            var entity = new UnitEntity();
            _simulation.AddEntity(entity);

            var retrieved = _simulation.GetEntity(entity.EntityId);

            Assert.AreSame(entity, retrieved);
        }

        [Test]
        public void RemoveEntity_RemovesFromSimulation()
        {
            var entity = new UnitEntity();
            _simulation.AddEntity(entity);
            int entityId = entity.EntityId;

            _simulation.RemoveEntity(entityId);

            Assert.IsNull(_simulation.GetEntity(entityId));
            Assert.AreEqual(0, _simulation.Entities.Count);
        }

        #endregion

        #region State Hash

        [Test]
        public void GetStateHash_SameState_ReturnsSameHash()
        {
            _simulation.Initialize(12345);
            var entity = _simulation.SpawnEntity<UnitEntity>(0);
            entity.Position = new FPVector3(10, 0, 10);

            long hash1 = _simulation.GetStateHash();
            long hash2 = _simulation.GetStateHash();

            Assert.AreEqual(hash1, hash2);
        }

        [Test]
        public void GetStateHash_DifferentState_ReturnsDifferentHash()
        {
            _simulation.Initialize(12345);
            var entity = _simulation.SpawnEntity<UnitEntity>(0);
            entity.Position = new FPVector3(10, 0, 10);

            long hash1 = _simulation.GetStateHash();

            entity.Position = new FPVector3(20, 0, 20);
            long hash2 = _simulation.GetStateHash();

            Assert.AreNotEqual(hash1, hash2);
        }

        #endregion

        #region Snapshot

        [Test]
        public void CreateSnapshot_ReturnsValidSnapshot()
        {
            var entity = _simulation.SpawnEntity<UnitEntity>(0);
            entity.Position = new FPVector3(5, 0, 5);

            var snapshot = _simulation.CreateSnapshot(0);

            Assert.IsNotNull(snapshot);
            Assert.AreEqual(0, snapshot.Tick);
        }

        #endregion

        #region Determinism

        [Test]
        public void SameInputs_ProduceSameState()
        {
            // First simulation
            var sim1 = new Simulation(50);
            sim1.Initialize(12345);
            var entity1 = sim1.SpawnEntity<UnitEntity>(0);
            entity1.Position = new FPVector3(0, 0, 0);

            var commands = new List<ICommand>
            {
                new MoveCommand(0, 0, FP64.FromFloat(10).RawValue, 0, FP64.FromFloat(10).RawValue)
            };

            for (int i = 0; i < 100; i++)
            {
                sim1.Tick(i == 0 ? commands : new List<ICommand>());
            }

            // Second simulation (same input)
            var sim2 = new Simulation(50);
            sim2.Initialize(12345);
            var entity2 = sim2.SpawnEntity<UnitEntity>(0);
            entity2.Position = new FPVector3(0, 0, 0);

            for (int i = 0; i < 100; i++)
            {
                sim2.Tick(i == 0 ? commands : new List<ICommand>());
            }

            // State should be identical
            Assert.AreEqual(sim1.GetStateHash(), sim2.GetStateHash());
            Assert.AreEqual(entity1.Position.x.RawValue, entity2.Position.x.RawValue);
            Assert.AreEqual(entity1.Position.z.RawValue, entity2.Position.z.RawValue);
        }

        [Test]
        public void DifferentInputs_ProduceDifferentState()
        {
            // First simulation
            var sim1 = new Simulation(50);
            sim1.Initialize(12345);
            var entity1 = sim1.SpawnEntity<UnitEntity>(0);
            entity1.Position = new FPVector3(0, 0, 0);

            var commands1 = new List<ICommand>
            {
                new MoveCommand(0, 0, FP64.FromFloat(10).RawValue, 0, 0)
            };
            sim1.Tick(commands1);

            // Second simulation (different input)
            var sim2 = new Simulation(50);
            sim2.Initialize(12345);
            var entity2 = sim2.SpawnEntity<UnitEntity>(0);
            entity2.Position = new FPVector3(0, 0, 0);

            var commands2 = new List<ICommand>
            {
                new MoveCommand(0, 0, 0, 0, FP64.FromFloat(10).RawValue)
            };
            sim2.Tick(commands2);

            // State should differ (moved in different directions)
            Assert.AreNotEqual(entity1.TargetPosition.x.RawValue, entity2.TargetPosition.x.RawValue);
        }

        #endregion

        #region Reset

        [Test]
        public void Reset_ClearsState()
        {
            var entity = _simulation.SpawnEntity<UnitEntity>(0);
            _simulation.Tick(new List<ICommand>());
            _simulation.Tick(new List<ICommand>());

            _simulation.Reset();

            Assert.AreEqual(0, _simulation.Entities.Count);
        }

        #endregion
    }

    /// <summary>
    /// UnitEntity tests
    /// </summary>
    [TestFixture]
    public class UnitEntityTests
    {
        [Test]
        public void ApplyMoveCommand_SetsTargetPosition()
        {
            var entity = new UnitEntity { OwnerId = 0 };
            var cmd = new MoveCommand(0, 0, FP64.FromFloat(10).RawValue, 0, FP64.FromFloat(5).RawValue);

            entity.ApplyCommand(cmd);

            Assert.IsTrue(entity.IsMoving);
            Assert.AreEqual(10.0f, entity.TargetPosition.x.ToFloat(), 0.01f);
            Assert.AreEqual(5.0f, entity.TargetPosition.z.ToFloat(), 0.01f);
        }

        [Test]
        public void ApplyCommand_WrongOwner_DoesNothing()
        {
            var entity = new UnitEntity { OwnerId = 0 };
            var cmd = new MoveCommand(1, 0, FP64.FromFloat(10).RawValue, 0, 0); // Different player

            entity.ApplyCommand(cmd);

            Assert.IsFalse(entity.IsMoving);
        }

        [Test]
        public void SimulationUpdate_MovesTowardsTarget()
        {
            var entity = new UnitEntity { OwnerId = 0 };
            entity.Position = FPVector3.Zero;
            entity.TargetPosition = new FPVector3(FP64.FromFloat(10), FP64.Zero, FP64.Zero);
            entity.IsMoving = true;

            // 100ms update
            entity.SimulationUpdate(100);

            // Position should have changed
            Assert.Greater(entity.Position.x.ToFloat(), 0);
        }

        [Test]
        public void SimulationUpdate_ReachesTarget_StopsMoving()
        {
            var entity = new UnitEntity { OwnerId = 0 };
            entity.Position = FPVector3.Zero;
            entity.TargetPosition = new FPVector3(FP64.FromFloat(0.05f), FP64.Zero, FP64.Zero);
            entity.IsMoving = true;

            // Sufficient time update
            entity.SimulationUpdate(1000);

            Assert.IsFalse(entity.IsMoving);
        }

        [Test]
        public void SerializeDeserialize_PreservesState()
        {
            var entity = new UnitEntity { OwnerId = 5 };
            entity.Position = new FPVector3(FP64.FromFloat(10), FP64.FromFloat(5), FP64.FromFloat(3));
            entity.CurrentHealth = 75;
            entity.IsMoving = true;

            byte[] data = entity.SerializeState();
            var restored = new UnitEntity();
            restored.DeserializeState(data);

            Assert.AreEqual(entity.OwnerId, restored.OwnerId);
            Assert.AreEqual(entity.Position.x.RawValue, restored.Position.x.RawValue);
            Assert.AreEqual(entity.CurrentHealth, restored.CurrentHealth);
            Assert.AreEqual(entity.IsMoving, restored.IsMoving);
        }

        [Test]
        public void GetStateHash_SameState_SameHash()
        {
            var entity1 = new UnitEntity();
            entity1.Position = new FPVector3(10, 5, 3);
            entity1.CurrentHealth = 50;

            var entity2 = new UnitEntity();
            entity2.Position = new FPVector3(10, 5, 3);
            entity2.CurrentHealth = 50;

            Assert.AreEqual(entity1.GetStateHash(), entity2.GetStateHash());
        }

        [Test]
        public void TakeDamage_ReducesHealth()
        {
            var entity = new UnitEntity();
            entity.CurrentHealth = 100;

            entity.TakeDamage(30);

            Assert.AreEqual(70, entity.CurrentHealth);
        }

        [Test]
        public void TakeDamage_AtZero_DoesNotGoNegative()
        {
            var entity = new UnitEntity();
            entity.CurrentHealth = 50;

            entity.TakeDamage(100);

            Assert.AreEqual(0, entity.CurrentHealth);
        }
    }
}
