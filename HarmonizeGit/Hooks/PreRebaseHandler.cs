using FishingWithGit;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGit
{
    public class PreRebaseHandler : TypicalHandlerBase
    {
        public PreRebaseHandler(HarmonizeGitBase harmonize)
            : base(harmonize)
        {
            this.NeedsConfig = false;
        }

        public override async Task<bool> Handle(string[] args)
        {
            var rebaseArgs = new RebaseArgs(args);

            List<string> strandedCommitShas;
            using (var repo = new Repository(this.harmonize.TargetPath))
            {
                var targetBranch = repo.Branches[rebaseArgs.TargetBranch];
                if (targetBranch == null)
                {
                    harmonize.WriteLine($"Target branch {rebaseArgs.TargetBranch} could not be found.");
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
            Commit targetCommit,
            Commit referenceCommit,
            out IEnumerable<Commit> strandedCommits)
        {
            var divergence = repo.ObjectDatabase.CalculateHistoryDivergence(targetCommit, referenceCommit);
            var ancestor = divergence.CommonAncestor;
            if (ancestor == null)
            {
                harmonize.WriteLine("No common ancestor found.");
                strandedCommits = null;
                return false;
            }

            harmonize.WriteLine($"Getting stranded commits between {targetCommit.Sha} and {ancestor.Sha}");
            strandedCommits = repo.GetPotentiallyStrandedCommits(
                targetCommit,
                ancestor);
            foreach (var commit in strandedCommits)
            {
                harmonize.WriteLine($"   {commit.Sha} -- {commit.MessageShort}");
            }
            return true;
        }
    }
}
