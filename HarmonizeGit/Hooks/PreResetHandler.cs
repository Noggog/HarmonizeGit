using FishingWithGit;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HarmonizeGit
{
    public class PreResetHandler : TypicalHandlerBase
    {
        ResetArgs args;
        public override IGitHookArgs Args => args;

        public PreResetHandler(HarmonizeGitBase harmonize,ResetArgs args)
            : base(harmonize)
        {
            this.NeedsConfig = false;
            this.args = args;
        }

        public override async Task<bool> Handle()
        {
            List<Commit> strandedCommits;
            var repo = this.harmonize.Repo;
            this.harmonize.Logger.WriteLine("Getting stranded commits: ");
            Commit targetCommit = repo.Lookup<Commit>(args.TargetSha);
            if (targetCommit == null)
            {
                this.harmonize.Logger.WriteLine($"Target reset commit did not exist: {args.TargetSha}");
            }

            strandedCommits = repo.GetPotentiallyStrandedCommits(
                repo.Head.Tip,
                targetCommit).ToList();
            foreach (var commit in strandedCommits)
            {
                this.harmonize.Logger.WriteLine($"   {commit.Sha} -- {commit.MessageShort}");
            }

            return await DoResetTasks(harmonize, repo, strandedCommits);
        }

        public static async Task<bool> BlockIfChildrenAreUsing(
            HarmonizeGitBase harmonize,
            IEnumerable<string> strandedCommitShas)
        {
            // See if children are using stranded commits
            var childUsages = await harmonize.ChildLoader.GetChildUsages(strandedCommitShas);
            if (childUsages.ChildRepos.Count > 0)
            {
                #region Print
                List<string> errs = new List<string>();
                errs.Add("Child repositories marked target commits as used.  Stopping.");
                errs.Add("");
                errs.Add("Repositories:");
                foreach (var usage in childUsages.ChildRepos.OrderBy((str) => str))
                {
                    errs.Add($"   {usage}");
                }

                errs.Add("Some Stranded Commits:");
                foreach (var usage in childUsages.UsedCommits)
                {
                    errs.Add($"   {usage}");
                }
                foreach (var err in errs)
                {
                    harmonize.Logger.WriteLine(err, error: true);
                }

                var ret = harmonize.Logger.LogErrorRetry(
                    string.Join("\n", errs),
                    "Confirm Safety Bypass", Settings.Instance.ShowMessageBoxes);
                if (ret == null)
                {
                    return await BlockIfChildrenAreUsing(harmonize, strandedCommitShas);
                }
                return ret.Value;
                #endregion
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
                harmonize.Logger.WriteLine("Removing lost commits from parent databases.");
                var usages = strandedCommits.SelectMany(
                    (commit) =>
                    {
                        return harmonize.ChildLoader.GetUsagesFromConfig(
                            harmonize.ConfigLoader.GetConfigFromRepo(
                                repo,
                                commit),
                            commit.Sha);
                    });
                await harmonize.ChildLoader.RemoveChildEntries(usages);
            }
            else
            {
                harmonize.Logger.WriteLine("No config.  Skipping unregister step.");
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
