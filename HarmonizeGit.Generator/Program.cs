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
    public class Program
    {
        static void Main(string[] args)
        {
            LoquiGenerator gen = new LoquiGenerator(
                new DirectoryInfo("../../../HarmonizeGitCloner"))
            {
                DefaultNamespace = "HarmonizeGitCloner",
                RaisePropertyChangedDefault = false,
                ProtocolDefault = new ProtocolKey("HarmonizeGitCloner")
            };

            // Add Projects
            gen.AddProjectToModify(
                new FileInfo(Path.Combine(gen.CommonGenerationFolder.FullName, "HarmonizeGitCloner.csproj")));

            gen.AddProtocol(
                new ProtocolGeneration(
                    gen,
                    gen.ProtocolDefault));

            gen.Generate();
        }
    }
}
