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
        BranchArgs args;
        public override IGitHookArgs Args => args;

        public PreBranchHandler(HarmonizeGitBase harmonize, BranchArgs args)
            : base(harmonize)
        {
            this.args = args;
        }

        public override async Task<bool> Handle()
        {
            if (!args.Deleting) return true;

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
