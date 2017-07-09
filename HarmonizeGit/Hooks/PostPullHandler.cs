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
            using (var repo = new Repository(this.harmonize.TargetPath))
            {
                var ancestorCommit = repo.Lookup<Commit>(args.AncestorSha);
                if (ancestorCommit == null)
                {
                    this.harmonize.WriteLine($"Ancestor commit did not exist: {args.AncestorSha}");
                    return false;
                }

                await repo.InsertStrandedCommitsIntoParent(
                    this.harmonize,
                    repo.Head.Tip,
                    ancestorCommit);
            }

            return harmonize.SyncParentReposToSha(args.TargetSha);
        }
    }
}
