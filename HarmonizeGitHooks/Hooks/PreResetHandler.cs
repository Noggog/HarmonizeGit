using FishingWithGit;
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

        public override async Task<bool> Handle(string[] args)
        {
            ResetArgs resetArgs = new ResetArgs(args);

            List<Commit> strandedCommits;
            using (var repo = new Repository(this.harmonize.TargetPath))
            {
                this.harmonize.WriteLine("Getting stranded commits: ");
                Commit targetCommit = repo.Lookup<Commit>(resetArgs.TargetSha);
                if (targetCommit == null)
                {
                    this.harmonize.WriteLine($"Target reset commit did not exist: {resetArgs.TargetSha}");
                }

                strandedCommits = repo.GetPotentiallyStrandedCommits(
                    repo.Head.Tip,
                    targetCommit).ToList();
                foreach (var commit in strandedCommits)
                {
                    this.harmonize.WriteLine($"   {commit.Sha} -- {commit.MessageShort}");
                }

                return await DoResetTasks(harmonize, repo, strandedCommits);
            }
        }

        public static async Task<bool> BlockIfChildrenAreUsing(
            HarmonizeGitBase harmonize,
            IEnumerable<string> strandedCommitShas)
        {
            // See if children are using stranded commits
            var childUsages = await harmonize.ChildLoader.GetChildUsages(strandedCommitShas, 10);
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
            return true;
        }

        public static async Task RemoveFromParentDatabase(
            HarmonizeGitBase harmonize,
            Repository repo,
            IEnumerable<Commit> strandedCommits)
        {
            // Unregister lost commits from parents
            if (harmonize.Config != null)
            {
                harmonize.WriteLine("Removing lost commits from parent databases.");
                var usages = strandedCommits.SelectMany(
                    (commit) =>
                    {
                        return harmonize.ChildLoader.GetConfigUsages(
                            harmonize.ConfigLoader.GetConfigFromRepo(
                                repo,
                                commit),
                            commit.Sha);
                    });
                await harmonize.ChildLoader.RemoveChildEntries(usages);
            }
            else
            {
                harmonize.WriteLine("No config.  Skipping unregister step.");
            }
        }

        public static async Task<bool> DoResetTasks(
            HarmonizeGitBase harmonize,
            Repository repo,
            IEnumerable<Commit> strandedCommits)
        {
            if (!(await BlockIfChildrenAreUsing(harmonize, strandedCommits.Select((c) => c.Sha)))) return false;
            await RemoveFromParentDatabase(harmonize, repo, strandedCommits);
            return true;
        }
    }
}
