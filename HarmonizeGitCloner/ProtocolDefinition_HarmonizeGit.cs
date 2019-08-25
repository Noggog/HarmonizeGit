using Loqui;

namespace Loqui
{
    public class ProtocolDefinition_HarmonizeGit : IProtocolRegistration
    {
        public readonly static ProtocolKey ProtocolKey = new ProtocolKey("HarmonizeGit");
        void IProtocolRegistration.Register() => Register();
        public static void Register()
        {
        }
    }
}
