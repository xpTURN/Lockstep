using NUnit.Framework;
using Lockstep.Core;
using Lockstep.Core.Impl;

namespace Lockstep.Tests
{
    /// <summary>
    /// Command serialization/deserialization tests
    /// </summary>
    [TestFixture]
    public class CommandTests
    {
        #region EmptyCommand

        [Test]
        public void EmptyCommand_Serialize_Deserialize_PreservesData()
        {
            var original = new EmptyCommand(1, 100);

            byte[] data = original.Serialize();
            var restored = new EmptyCommand();
            restored.Deserialize(data);

            Assert.AreEqual(original.PlayerId, restored.PlayerId);
            Assert.AreEqual(original.Tick, restored.Tick);
            Assert.AreEqual(original.CommandType, restored.CommandType);
        }

        [Test]
        public void EmptyCommand_HasCorrectTypeId()
        {
            var cmd = new EmptyCommand();
            Assert.AreEqual(EmptyCommand.TYPE_ID, cmd.CommandType);
            Assert.AreEqual(0, cmd.CommandType);
        }

        #endregion

        #region MoveCommand

        [Test]
        public void MoveCommand_Serialize_Deserialize_PreservesData()
        {
            var original = new MoveCommand(2, 50, 1000L, 2000L, 3000L);

            byte[] data = original.Serialize();
            var restored = new MoveCommand();
            restored.Deserialize(data);

            Assert.AreEqual(original.PlayerId, restored.PlayerId);
            Assert.AreEqual(original.Tick, restored.Tick);
            Assert.AreEqual(original.TargetX, restored.TargetX);
            Assert.AreEqual(original.TargetY, restored.TargetY);
            Assert.AreEqual(original.TargetZ, restored.TargetZ);
        }

        [Test]
        public void MoveCommand_HasCorrectTypeId()
        {
            var cmd = new MoveCommand();
            Assert.AreEqual(MoveCommand.TYPE_ID, cmd.CommandType);
            Assert.AreEqual(1, cmd.CommandType);
        }

        [Test]
        public void MoveCommand_NegativeCoordinates_WorksCorrectly()
        {
            var original = new MoveCommand(1, 10, -500L, -1000L, -1500L);

            byte[] data = original.Serialize();
            var restored = new MoveCommand();
            restored.Deserialize(data);

            Assert.AreEqual(original.TargetX, restored.TargetX);
            Assert.AreEqual(original.TargetY, restored.TargetY);
            Assert.AreEqual(original.TargetZ, restored.TargetZ);
        }

        #endregion

        #region ActionCommand

        [Test]
        public void ActionCommand_Serialize_Deserialize_PreservesData()
        {
            var original = new ActionCommand(3, 75, 5, 10)
            {
                PositionX = 100L,
                PositionY = 200L,
                PositionZ = 300L
            };

            byte[] data = original.Serialize();
            var restored = new ActionCommand();
            restored.Deserialize(data);

            Assert.AreEqual(original.PlayerId, restored.PlayerId);
            Assert.AreEqual(original.Tick, restored.Tick);
            Assert.AreEqual(original.ActionId, restored.ActionId);
            Assert.AreEqual(original.TargetEntityId, restored.TargetEntityId);
            Assert.AreEqual(original.PositionX, restored.PositionX);
            Assert.AreEqual(original.PositionY, restored.PositionY);
            Assert.AreEqual(original.PositionZ, restored.PositionZ);
        }

        [Test]
        public void ActionCommand_HasCorrectTypeId()
        {
            var cmd = new ActionCommand();
            Assert.AreEqual(ActionCommand.TYPE_ID, cmd.CommandType);
            Assert.AreEqual(2, cmd.CommandType);
        }

        #endregion

        #region CommandFactory

        [Test]
        public void CommandFactory_CreateCommand_EmptyCommand()
        {
            var factory = new CommandFactory();
            var cmd = factory.CreateCommand(EmptyCommand.TYPE_ID);

            Assert.IsNotNull(cmd);
            Assert.IsInstanceOf<EmptyCommand>(cmd);
        }

        [Test]
        public void CommandFactory_CreateCommand_MoveCommand()
        {
            var factory = new CommandFactory();
            var cmd = factory.CreateCommand(MoveCommand.TYPE_ID);

            Assert.IsNotNull(cmd);
            Assert.IsInstanceOf<MoveCommand>(cmd);
        }

        [Test]
        public void CommandFactory_CreateCommand_ActionCommand()
        {
            var factory = new CommandFactory();
            var cmd = factory.CreateCommand(ActionCommand.TYPE_ID);

            Assert.IsNotNull(cmd);
            Assert.IsInstanceOf<ActionCommand>(cmd);
        }

        [Test]
        public void CommandFactory_DeserializeCommand_WorksCorrectly()
        {
            var factory = new CommandFactory();

            var original = new MoveCommand(1, 100, 500L, 600L, 700L);
            byte[] data = original.Serialize();

            var restored = factory.DeserializeCommand(data);

            Assert.IsNotNull(restored);
            Assert.IsInstanceOf<MoveCommand>(restored);

            var moveCmd = (MoveCommand)restored;
            Assert.AreEqual(original.PlayerId, moveCmd.PlayerId);
            Assert.AreEqual(original.Tick, moveCmd.Tick);
            Assert.AreEqual(original.TargetX, moveCmd.TargetX);
        }

        #endregion

        #region Determinism Tests

        [Test]
        public void Serialization_IsDeterministic()
        {
            var cmd = new MoveCommand(1, 100, 12345L, 67890L, 11111L);

            byte[] data1 = cmd.Serialize();
            byte[] data2 = cmd.Serialize();

            Assert.AreEqual(data1.Length, data2.Length);
            for (int i = 0; i < data1.Length; i++)
            {
                Assert.AreEqual(data1[i], data2[i], $"Byte {i} differs");
            }
        }

        [Test]
        public void MultipleSerializeDeserialize_PreservesData()
        {
            var original = new ActionCommand(5, 200, 10, 20)
            {
                PositionX = 999L,
                PositionY = 888L,
                PositionZ = 777L
            };

            // Multiple serialize/deserialize
            byte[] data = original.Serialize();
            var temp = new ActionCommand();
            temp.Deserialize(data);

            data = temp.Serialize();
            var final = new ActionCommand();
            final.Deserialize(data);

            Assert.AreEqual(original.PlayerId, final.PlayerId);
            Assert.AreEqual(original.ActionId, final.ActionId);
            Assert.AreEqual(original.PositionX, final.PositionX);
        }

        #endregion
    }
}
