using FishingWithGit;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGit
{
    class PostPullHandler : TypicalHandlerBase
    {
        public PostPullHandler(HarmonizeGitBase harmonize)
            : base(harmonize)
        {
        }

        public override async Task<bool> Handle(string[] args)
        {
            PullArgs pullArgs = new PullArgs(args);

            using (var repo = new Repository(this.harmonize.TargetPath))
            {
                var ancestorCommit = repo.Lookup<Commit>(pullArgs.AncestorSha);
                if (ancestorCommit == null)
                {
                    this.harmonize.WriteLine($"Ancestor commit did not exist: {pullArgs.AncestorSha}");
                    return false;
                }

                await repo.InsertStrandedCommitsIntoParent(
                    this.harmonize,
                    repo.Head.Tip,
                    ancestorCommit);
            }

            return true;
        }
    }
}
