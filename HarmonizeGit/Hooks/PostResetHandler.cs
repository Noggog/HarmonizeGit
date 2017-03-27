using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FishingWithGit;
using LibGit2Sharp;

namespace HarmonizeGit
{
    class PostResetHandler : TypicalHandlerBase
    {
        public PostResetHandler(HarmonizeGitBase harmonize) 
            : base(harmonize)
        {
        }

        public override async Task<bool> Handle(string[] args)
        {
            ResetArgs resetArgs = new ResetArgs(args);
                harmonize.SyncParentRepos();

            using (var repo = new Repository(this.harmonize.TargetPath))
            {
                var startingCommit = repo.Lookup<Commit>(resetArgs.StartingSha);
                if (startingCommit == null)
                {
                    this.harmonize.WriteLine($"Starting commit did not exist {resetArgs.StartingSha}");
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
