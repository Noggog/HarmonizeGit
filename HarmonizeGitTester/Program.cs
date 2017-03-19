using HarmonizeGit;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGitTester
{
    class Program
    {
        static void Main(string[] args)
        {
            //var harmonize = new HarmonizeGitBase("C:\\Users\\Noggog\\Documents\\DynamicLeveledLists");
            //harmonize.ChildLoader.CheckAndSeed(
            //    "C:\\Users\\Noggog\\Documents\\Noggolloquy",
            //    "C:\\Users\\Noggog\\Documents\\DynamicLeveledLists").Wait();


            //var harmonize = new HarmonizeGitBase("D:\\Dropbox-Real\\Dropbox\\Harmonize-Child-Repo");
            //harmonize.ChildLoader.RemoveChildEntries(
            //    new ChildUsage[] { new ChildUsage()
            //    {
            //        ChildRepoPath = "D:\\Dropbox-Real\\Dropbox\\Harmonize-Child-Repo",
            //        ParentRepoPath = "D:\\Dropbox-Real\\Dropbox\\Harmonize-Parent-Repo",
            //        ParentSha = "39a2aadbbfde137650922d71652a70ce46c497c2",
            //        Sha = "8191ade505b1427e16e4d795c3fe080c52fd6eba"
            //    }});

            Run().Wait();
        }

        public static async Task Run()
        {
            var harmonize = new HarmonizeGitBase("D:\\Repos\\BlackInkCry");
            await harmonize.Handle(
               new string[] { "pre-checkout", "checkout", "6c865f86289e98798dbec7ab64bd72c5e12f8333", "ebba898d1fcae54c581c6d3fcb9b53b163c1ba56" });
        }
    }
}
