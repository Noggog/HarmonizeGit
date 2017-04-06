using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FishingWithGit;

namespace HarmonizeGit
{
    public class TakeHandler : TypicalHandlerBase
    {
        TakeArgs args;
        public override IGitHookArgs Args => args;

        public TakeHandler(HarmonizeGitBase harmonize, TakeArgs args)
            : base(harmonize)
        {
            this.args = args;
        }

        public override async Task<bool> Handle()
        {
            this.harmonize.SyncParentRepos();
            return true;
        }
    }
}
