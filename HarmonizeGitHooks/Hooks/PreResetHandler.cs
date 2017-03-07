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

                // Unregister lost commits from parents
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
            return true;
        }
    }
}
