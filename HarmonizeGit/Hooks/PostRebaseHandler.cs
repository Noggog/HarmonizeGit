using FishingWithGit;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGit
{
    public class PostRebaseHandler : TypicalHandlerBase
    {
        public PostRebaseHandler(HarmonizeGitBase harmonize)
            : base(harmonize)
        {
            this.NeedsConfig = false;
        }

        public override async Task<bool> Handle(string[] args)
        {
            var rebaseArgs = new RebaseInProgressArgs(args);
            using (var repo = new Repository(this.harmonize.TargetPath))
            {
                var originalCommit = repo.Lookup<Commit>(rebaseArgs.OriginalTipSha);
                if (originalCommit == null)
                {
                    harmonize.WriteLine($"Original commit {rebaseArgs.OriginalTipSha} could not be found.");
                    return false;
                }

                var landingCommit = repo.Lookup<Commit>(rebaseArgs.LandingSha);
                if (landingCommit == null)
                {
                    harmonize.WriteLine($"Target commit {rebaseArgs.LandingSha} could not be found.");
                    return false;
                }

                // Remove old commits from usage
                if (!PreRebaseHandler.GetStrandedCommits(
                    this.harmonize,
                    repo,
                    originalCommit,
                    landingCommit,
                    out IEnumerable<Commit> strandedCommits))
                {
                    return false;
                }

                await PreResetHandler.RemoveFromParentDatabase(
                    this.harmonize,
                    repo,
                    strandedCommits);

                // Insert new commits to usage
                if (!PreRebaseHandler.GetStrandedCommits(
                    this.harmonize,
                    repo,
                    repo.Head.Tip,
                    landingCommit,
                    out IEnumerable<Commit> newCommits))
                {
                    return false;
                }

                await this.harmonize.ChildLoader.InsertChildEntries(
                    this.harmonize.ChildLoader.GetUsages(
                        repo,
                        newCommits));
            }
            return true;
        }
    }
}
