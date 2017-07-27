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
        RebaseArgs args;
        public override IGitHookArgs Args => args;

        public PreRebaseHandler(HarmonizeGitBase harmonize, RebaseArgs args)
            : base(harmonize)
        {
            this.NeedsConfig = false;
            this.args = args;
        }

        public override async Task<bool> Handle()
        {
            List<string> strandedCommitShas;
            var repo = this.harmonize.Repo;
            var targetBranch = repo.Branches[args.TargetBranch];
            if (targetBranch == null)
            {
                harmonize.Logger.WriteLine($"Target branch {args.TargetBranch} could not be found.");
                return false;
            }
            if (!GetStrandedCommits(
                this.harmonize,
                repo,
                repo.Head.Tip,
                targetBranch.Tip,
                out IEnumerable<Commit> strandedCommits)) return false;

            strandedCommitShas = strandedCommits.Select((c) => c.Sha).ToList();

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
                harmonize.Logger.WriteLine("No common ancestor found.");
                strandedCommits = null;
                return false;
            }

            harmonize.Logger.WriteLine($"Getting stranded commits between {targetCommit.Sha} and {ancestor.Sha}");
            strandedCommits = repo.GetPotentiallyStrandedCommits(
                targetCommit,
                ancestor);
            foreach (var commit in strandedCommits)
            {
                harmonize.Logger.WriteLine($"   {commit.Sha} -- {commit.MessageShort}");
            }
            return true;
        }
    }
}
