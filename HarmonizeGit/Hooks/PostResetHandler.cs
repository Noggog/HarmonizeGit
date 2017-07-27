using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FishingWithGit;
using LibGit2Sharp;

namespace HarmonizeGit
{
    public class PostResetHandler : TypicalHandlerBase
    {
        ResetArgs args;
        public override IGitHookArgs Args => args;

        public PostResetHandler(HarmonizeGitBase harmonize, ResetArgs args)
            : base(harmonize)
        {
            this.args = args;
        }

        public override async Task<bool> Handle()
        {
            if (!harmonize.SyncParentRepos()) return false;

            var repo = this.harmonize.Repo;
            var startingCommit = repo.Lookup<Commit>(args.StartingSha);
            if (startingCommit == null)
            {
                return harmonize.Logger.LogError(
                    $"Starting commit did not exist {args.StartingSha}",
                    "Error",
                    Settings.Instance.ShowMessageBoxes);
            }

            await repo.InsertStrandedCommitsIntoParent(
                this.harmonize,
                repo.Head.Tip,
                startingCommit);
            return true;
        }
    }
}
