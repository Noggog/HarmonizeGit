using HarmonizeGit;
using LibGit2Sharp;
using Noggog;
using Noggog.Utility.ArgsQuerying;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGitCloner
{
    class Program
    {
        static ArgsQueryManager argsMgr;

        static void Main(string[] args)
        {
            argsMgr = new ArgsQueryConsoleManager(args);
            var toDo = argsMgr.Prompt(@"Desired job:
  1) Mass clone from a Spec File
  2) Create Spec File from existing repo");
            switch (toDo)
            {
                case "1":
                case "-massclone":
                    MassClone();
                    break;
                case "2":
                case "-createspec":
                    CreateSpec();
                    break;
                default:
                    break;
            }
        }

        private static void MassClone()
        {
            var targetCloneFolder = argsMgr.Prompt("Enter folder to put clones into:");

            var targetFolder = new DirectoryPath(targetCloneFolder);
            if (!targetFolder.Exists)
            {
                System.Console.WriteLine($"Creating target folder: {targetFolder.Path}");
                targetFolder.Directory.Create();
            }

            var specPath = argsMgr.Prompt("Enter clone spec file path:");
            var spec = CloneSpec.Create_XML(specPath);
            HashSet<Clone> targets = new HashSet<Clone>(spec.ExplicitClones);

            if (targets.Count == 0)
            {
                System.Console.WriteLine("No work to be done as spec was empty.");
                return;
            }

            foreach (var target in targets)
            {
                System.Console.WriteLine($"Cloning {target.Nickname} from {target.ClonePath}.");
                Repository.Clone(target.ClonePath, Path.Combine(targetFolder.Path, target.Nickname));
                System.Console.WriteLine("Cloned.");
            }

            System.Console.WriteLine("DONE.  Press enter to exit.");
            System.Console.ReadLine();
        }

        private static void CreateSpec()
        {
            var prototypeRepo = new DirectoryPath(argsMgr.Prompt("Enter repo path to base spec on:"));

            if (!Repository.IsValid(prototypeRepo.Path))
            {
                System.Console.WriteLine("Path did not lead to a valid repo.");
                return;
            }

            var harmonize = new HarmonizeGitBase(prototypeRepo.Path);
            harmonize.Init();
            List<(string path, Clone clone)> itemsToAdd = new List<(string path, Clone clone)>();
            if (argsMgr.PromptBool("Include target repo itself in spec?:"))
            {
                var clone = new Clone()
                {
                    ClonePath = GetRemote(harmonize.Repo),
                    Nickname = prototypeRepo.Name
                };
                itemsToAdd.Add((
                    prototypeRepo.Path,
                    clone));
            }
            foreach (var listing in harmonize.Config.ParentRepos)
            {
                DirectoryPath dir = new DirectoryPath(listing.Path, harmonize.TargetPath);
                var parentRepo = harmonize.RepoLoader.GetRepo(dir.Path);
                itemsToAdd.Add((listing.Path,
                    new Clone()
                    {
                        ClonePath = GetRemote(parentRepo),
                        Nickname = listing.Nickname
                    }));
            }

            foreach (var pathGroup in itemsToAdd.GroupBy((g) => g.path))
            {
                if (pathGroup.CountGreaterThan(1))
                {
                    System.Console.Error.WriteLine($"Harmonize config listed the same parent twice: {pathGroup.Key}");
                    return;
                }
            }

            CloneSpec cloneSpec = new CloneSpec();
            cloneSpec.ExplicitClones.Add(itemsToAdd.Select((c) => c.clone));

            cloneSpec.Write_XML(argsMgr.Prompt("Enter path to export spec to"));

            System.Console.WriteLine("DONE.  Press enter to exit.");
            System.Console.ReadLine();
        }

        private static string GetRemote(Repository repo)
        {
            var origins = repo.Network.Remotes.ToList();
            if (origins.Count == 0)
            {
                System.Console.Error.WriteLine($"{repo.Info.Path} did not have any remotes.");
                return null;
            }
            if (origins.Count > 1)
            {
                System.Console.Error.WriteLine($"{repo.Info.Path} had more than one remote.");
                return null;
            }
            return origins[0].Url;
        }
    }
}
