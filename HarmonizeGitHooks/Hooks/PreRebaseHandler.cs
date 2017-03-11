using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGitHooks
{
    class PreRebaseHandler : TypicalHandlerBase
    {
        public PreRebaseHandler(HarmonizeGitBase harmonize)
            : base(harmonize)
        {
            this.NeedsConfig = false;
        }

        public override async Task<bool> Handle(List<string> args)
        {
            if (args.Count < 1)
            {
                this.harmonize.WriteLine("No branch name argument.");
                return false;
            }

            using (var repo = new Repository(this.harmonize.TargetPath))
            {
                var targetBranch = repo.Branches[args[0]];
                if (targetBranch == null)
                {
                    this.harmonize.WriteLine($"Target branch {args[0]} could not be found.");
                    return false;
                }
                var divergence = repo.ObjectDatabase.CalculateHistoryDivergence(repo.Head.Tip, targetBranch.Tip);
                var ancestor = divergence.CommonAncestor;
                if (ancestor == null)
                {
                    this.harmonize.WriteLine("No common ancestor found.");
                    return false;
                }

                this.harmonize.WriteLine("Getting stranded commits: ");
                var strandedCommits = repo.GetPotentiallyStrandedCommits(ancestor.Sha);
                foreach (var commit in strandedCommits)
                {
                    this.harmonize.WriteLine($"   {commit.Sha} -- {commit.MessageShort}");
                }
                return await PreResetHandler.DoResetTasks(
                    this.harmonize,
                    repo,
                    strandedCommits);
            }
        }
    }
}
