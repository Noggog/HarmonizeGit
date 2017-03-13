using FishingWithGit;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGitHooks
{
    class PostRebaseHandler : TypicalHandlerBase
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
                var originalCommit = repo.Lookup<Commit>(rebaseArgs.OriginalSha);
                if (originalCommit == null)
                {
                    harmonize.WriteLine($"Original commit {rebaseArgs.OriginalSha} could not be found.");
                    return false;
                }

                var targetCommit = repo.Lookup<Commit>(rebaseArgs.TargetSha);
                if (targetCommit == null)
                {
                    harmonize.WriteLine($"Target commit {rebaseArgs.TargetSha} could not be found.");
                    return false;
                }

                // Remove old commits from usage
                if (!PreRebaseHandler.GetStrandedCommits(
                    this.harmonize,
                    repo,
                    originalCommit,
                    targetCommit,
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
                    targetCommit,
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
