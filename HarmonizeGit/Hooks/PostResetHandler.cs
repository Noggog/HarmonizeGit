using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FishingWithGit;

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
            // ToDo
            // Add any new usages to parent repos, if reset was forward, not backward

            harmonize.SyncParentRepos();
            return true;
        }
    }
}
