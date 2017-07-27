using FishingWithGit;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                this.harmonize.Logger.WriteLine($"Ancestor commit did not exist: {args.AncestorSha}", error: true);
                return false;
            }

            await repo.InsertStrandedCommitsIntoParent(
                this.harmonize,
                repo.Head.Tip,
                ancestorCommit);

            return harmonize.SyncParentReposToSha(args.TargetSha);
        }
    }
}
