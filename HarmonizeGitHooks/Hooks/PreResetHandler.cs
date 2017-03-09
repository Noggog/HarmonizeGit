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

                // See if children are using stranded commits
                var childUsages = await this.harmonize.ChildLoader.GetChildUsages(strandedCommits.Select((c) => c.Sha), 10);
                if (childUsages.Item2.Count > 0)
                {
                    #region Print
                    this.harmonize.WriteLine("Repositories:");
                    foreach (var usage in childUsages.Item2.OrderBy((str) => str))
                    {
                        this.harmonize.WriteLine($"   {usage}");
                    }

                    this.harmonize.WriteLine("Some Stranded Commits:");
                    foreach (var usage in childUsages.Item1)
                    {
                        this.harmonize.WriteLine($"   {usage}");
                    }
                    this.harmonize.WriteLine("Child repositories marked stranded commits as used.  Stopping.");
                    #endregion
                    return false;
                }

                // Unregister lost commits from parents
                if (this.harmonize.Config != null)
                {
                    foreach (var commit in strandedCommits)
                    {
                        await this.harmonize.ChildLoader.RemoveChildEntries(
                            this.harmonize.ChildLoader.GetConfigUsages(
                                this.harmonize.ConfigLoader.GetConfigFromRepo(
                                    repo,
                                    commit),
                                commit.Sha));
                    }
                }
                else
                {
                    this.harmonize.WriteLine("No config.  Skipping unregister step.");
                }
            }
            return true;
        }
    }
}
