using Loqui;
using Loqui.Generation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGit.Generator
{
    class Program
    {
        static void Main(string[] args)
        {
            LoquiGenerator gen = new LoquiGenerator(
                new DirectoryInfo("../../../HarmonizeGit.RepoTranslator"))
            {
                DefaultNamespace = "HarmonizeGit.RepoTranslator",
                RaisePropertyChangedDefault = false,
                ProtocolDefault = new ProtocolKey("HarmonizeGitRepoTranslator")
            };

            // Add Projects
            gen.AddProjectToModify(
                new FileInfo(Path.Combine(gen.CommonGenerationFolder.FullName, "HarmonizeGit.RepoTranslator.csproj")));

            gen.AddProtocol(
                new ProtocolGeneration(
                    gen,
                    gen.ProtocolDefault));

            gen.Generate();
        }
    }
}
