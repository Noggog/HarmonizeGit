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

        public override async Task<bool> Handle(string[] args)
        {
            if (args.Length < 1)
            {
                this.harmonize.WriteLine("No branch name argument.");
                return false;
            }

            List<string> strandedCommitShas;
            using (var repo = new Repository(this.harmonize.TargetPath))
            {
                var targetBranch = repo.Branches[args[0]];
                if (targetBranch == null)
                {
                    harmonize.WriteLine($"Target branch {args[0]} could not be found.");
                    return false;
                }
                if (!GetStrandedCommits(
                    this.harmonize,
                    repo,
                    repo.Head.Tip,
                    targetBranch.Tip,
                    out IEnumerable<Commit> strandedCommits)) return false;

                strandedCommitShas = strandedCommits.Select((c) => c.Sha).ToList();
            }

            return await PreResetHandler.BlockIfChildrenAreUsing(
                this.harmonize,
                strandedCommitShas);
        }

        public static bool GetStrandedCommits(
            HarmonizeGitBase harmonize,
            Repository repo,
            Commit tipCommit,
            Commit targetCommit,
            out IEnumerable<Commit> strandedCommits)
        {
            var divergence = repo.ObjectDatabase.CalculateHistoryDivergence(tipCommit, targetCommit);
            var ancestor = divergence.CommonAncestor;
            if (ancestor == null)
            {
                harmonize.WriteLine("No common ancestor found.");
                strandedCommits = null;
                return false;
            }

            harmonize.WriteLine($"Getting stranded commits between {tipCommit.Sha} and {ancestor.Sha}");
            strandedCommits = repo.GetPotentiallyStrandedCommits(
                tipCommit,
                ancestor);
            foreach (var commit in strandedCommits)
            {
                harmonize.WriteLine($"   {commit.Sha} -- {commit.MessageShort}");
            }
            return true;
        }
    }
}
