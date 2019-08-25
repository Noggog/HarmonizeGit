using Loqui;
using Loqui.Generation;
using System;
using System.IO;

namespace HarmonizeGit.Generator
{
    class Program
    {
        static void Main(string[] args)
        {
            LoquiGenerator gen = new LoquiGenerator(typical: true)
            {
                RaisePropertyChangedDefault = false,
                NotifyingDefault = NotifyingType.ReactiveUI,
                ObjectCentralizedDefault = true,
                HasBeenSetDefault = false,
                ToStringDefault = false,
            };

            var proto = gen.AddProtocol(
                new ProtocolGeneration(
                    gen,
                    new ProtocolKey("HarmonizeGit"),
                    new DirectoryInfo("../../../../HarmonizeGit.GUI"))
                {
                    DefaultNamespace = "HarmonizeGit.GUI",
                });
            proto.AddProjectToModify(
                new FileInfo(Path.Combine(proto.GenerationFolder.FullName, "HarmonizeGit.GUI.csproj")));

            proto = gen.AddProtocol(
                new ProtocolGeneration(
                    gen,
                    new ProtocolKey("HarmonizeGitCloner"),
                    new DirectoryInfo("../../../../HarmonizeGitCloner"))
                {
                    DefaultNamespace = "HarmonizeGitCloner",
                });
            proto.AddProjectToModify(
                new FileInfo(Path.Combine(proto.GenerationFolder.FullName, "HarmonizeGitCloner.csproj")));

            gen.Generate().Wait();
        }
    }
}
