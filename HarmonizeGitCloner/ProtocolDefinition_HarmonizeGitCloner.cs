using Loqui;
using HarmonizeGitCloner;

namespace Loqui
{
    public class ProtocolDefinition_HarmonizeGitCloner : IProtocolRegistration
    {
        public readonly static ProtocolKey ProtocolKey = new ProtocolKey("HarmonizeGitCloner");
        public void Register()
        {
            LoquiRegistration.Register(HarmonizeGitCloner.Internals.CloneSpec_Registration.Instance);
            LoquiRegistration.Register(HarmonizeGitCloner.Internals.Clone_Registration.Instance);
        }
    }
}
