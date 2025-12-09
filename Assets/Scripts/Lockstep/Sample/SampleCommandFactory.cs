using xpTURN.Lockstep.Core;
using xpTURN.Lockstep.Core.Impl;

namespace xpTURN.Lockstep.Sample
{
    /// <summary>
    /// Command factory for sample game
    /// </summary>
    public class SampleCommandFactory : CommandFactory
    {
        public SampleCommandFactory()
        {
            // Default commands are registered in parent class
            // Register custom commands
            RegisterCommand<SkillCommand>(SkillCommand.TYPE_ID);
        }
    }
}
