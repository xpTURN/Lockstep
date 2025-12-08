using System.IO;
using Lockstep.Core.Impl;

namespace Lockstep.Sample
{
    /// <summary>
    /// Skill use command
    /// </summary>
    public class SkillCommand : CommandBase
    {
        public const int TYPE_ID = 100;
        public override int CommandType => TYPE_ID;

        public int SkillId { get; set; }
        public int TargetEntityId { get; set; }
        public long TargetX { get; set; }
        public long TargetY { get; set; }
        public long TargetZ { get; set; }

        public SkillCommand() : base() { }

        public SkillCommand(int playerId, int tick, int skillId, int targetEntityId = -1)
            : base(playerId, tick)
        {
            SkillId = skillId;
            TargetEntityId = targetEntityId;
        }

        protected override void SerializeData(BinaryWriter writer)
        {
            writer.Write(SkillId);
            writer.Write(TargetEntityId);
            writer.Write(TargetX);
            writer.Write(TargetY);
            writer.Write(TargetZ);
        }

        protected override void DeserializeData(BinaryReader reader)
        {
            SkillId = reader.ReadInt32();
            TargetEntityId = reader.ReadInt32();
            TargetX = reader.ReadInt64();
            TargetY = reader.ReadInt64();
            TargetZ = reader.ReadInt64();
        }
    }
}
