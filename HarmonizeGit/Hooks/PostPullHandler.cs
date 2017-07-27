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
    public class PostPullHandler : TypicalHandlerBase
    {
        PullArgs args;
        public override IGitHookArgs Args => args;

        public PostPullHandler(HarmonizeGitBase harmonize, PullArgs args)
            : base(harmonize)
        {
            this.args = args;
        }

        public override async Task<bool> Handle()
        {
            var repo = this.harmonize.Repo;
            var ancestorCommit = repo.Lookup<Commit>(args.AncestorSha);
            if (ancestorCommit == null)
            {
                return this.harmonize.Logger.LogError(
                    $"Ancestor commit did not exist: {args.AncestorSha}",
                    "Error",
                    Settings.Instance.ShowMessageBoxes);
            }

            await repo.InsertStrandedCommitsIntoParent(
                this.harmonize,
                repo.Head.Tip,
                ancestorCommit);

            return harmonize.SyncParentReposToSha(args.TargetSha);
        }
    }
}
