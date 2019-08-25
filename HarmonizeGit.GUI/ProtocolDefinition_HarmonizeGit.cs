using Loqui;
using HarmonizeGit.GUI;

namespace Loqui
{
    public class ProtocolDefinition_HarmonizeGit : IProtocolRegistration
    {
        public readonly static ProtocolKey ProtocolKey = new ProtocolKey("HarmonizeGit");
        void IProtocolRegistration.Register() => Register();
        public static void Register()
        {
            LoquiRegistration.Register(HarmonizeGit.GUI.Internals.Settings_Registration.Instance);
            LoquiRegistration.Register(HarmonizeGit.GUI.Internals.Repository_Registration.Instance);
        }
    }
}
