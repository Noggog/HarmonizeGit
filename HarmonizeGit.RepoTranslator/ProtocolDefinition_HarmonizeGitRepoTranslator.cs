using Loqui;
using HarmonizeGit.RepoTranslator;

namespace Loqui
{
    public class ProtocolDefinition_HarmonizeGitRepoTranslator : IProtocolRegistration
    {
        public readonly static ProtocolKey ProtocolKey = new ProtocolKey("HarmonizeGitRepoTranslator");
        public void Register()
        {
            LoquiRegistration.Register(HarmonizeGit.RepoTranslator.Internals.TranslatorSpec_Registration.Instance);
        }
    }
}
