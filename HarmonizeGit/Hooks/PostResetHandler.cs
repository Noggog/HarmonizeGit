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
            harmonize.SyncParentRepos();

            using (var repo = new Repository(this.harmonize.TargetPath))
            {
                var startingCommit = repo.Lookup<Commit>(args.StartingSha);
                if (startingCommit == null)
                {
                    this.harmonize.WriteLine($"Starting commit did not exist {args.StartingSha}");
                    return false;
                }

                await repo.InsertStrandedCommitsIntoParent(
                    this.harmonize,
                    repo.Head.Tip,
                    startingCommit);
            }
            return true;
        }
    }
}
