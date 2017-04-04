using FishingWithGit;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGit
{
    public class PreBranchHandler : TypicalHandlerBase
    {
        public PreBranchHandler(HarmonizeGitBase harmonize)
            : base(harmonize)
        {
        }

        public override async Task<bool> Handle(string[] args)
        {
            BranchArgs branchArgs = new BranchArgs(args);

            if (!branchArgs.Deleting) return true;

            using (var repo = new Repository(this.harmonize.TargetPath))
            {
                var strandedCommits = repo.GetPotentiallyStrandedCommits(repo.Head.Tip);
                return await PreResetHandler.DoResetTasks(
                    this.harmonize,
                    repo,
                    strandedCommits);
            }
        }
    }
}
