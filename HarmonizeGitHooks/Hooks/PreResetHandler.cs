using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGitHooks
{
    class PreResetHandler : TypicalHandlerBase
    {
        public PreResetHandler(HarmonizeGitBase harmonize)
            : base(harmonize)
        {
            this.NeedsConfig = false;
        }

        public override async Task<bool> Handle(List<string> args)
        {
            var curBranch = args[0];
            var targetSha = args[1];

            List<Commit> strandedCommits;
            using (var repo = new Repository(this.harmonize.TargetPath))
            {
                this.harmonize.WriteLine("Getting stranded commits: ");
                strandedCommits = repo.GetPotentiallyStrandedCommits(targetSha).ToList();
                foreach (var commit in strandedCommits)
                {
                    this.harmonize.WriteLine($"   {commit.Sha} -- {commit.MessageShort}");
                }

                return await DoResetTasks(harmonize, repo, strandedCommits);
            }
        }

        public static async Task<bool> DoResetTasks(
            HarmonizeGitBase harmonize,
            Repository repo,
            IEnumerable<Commit> strandedCommits)
        {
            // See if children are using stranded commits
            var childUsages = await harmonize.ChildLoader.GetChildUsages(strandedCommits.Select((c) => c.Sha), 10);
            if (childUsages.Item2.Count > 0)
            {
                #region Print
                harmonize.WriteLine("Repositories:");
                foreach (var usage in childUsages.Item2.OrderBy((str) => str))
                {
                    harmonize.WriteLine($"   {usage}");
                }

                harmonize.WriteLine("Some Stranded Commits:");
                foreach (var usage in childUsages.Item1)
                {
                    harmonize.WriteLine($"   {usage}");
                }
                harmonize.WriteLine("Child repositories marked stranded commits as used.  Stopping.");
                #endregion
                return false;
            }

            // Unregister lost commits from parents
            if (harmonize.Config != null)
            {
                harmonize.WriteLine("Removing lost commits from parent databases.");
                foreach (var commit in strandedCommits)
                {
                    await harmonize.ChildLoader.RemoveChildEntries(
                        harmonize.ChildLoader.GetConfigUsages(
                            harmonize.ConfigLoader.GetConfigFromRepo(
                                repo,
                                commit),
                            commit.Sha));
                }
            }
            else
            {
                harmonize.WriteLine("No config.  Skipping unregister step.");
            }
            return true;
        }
    }
}
