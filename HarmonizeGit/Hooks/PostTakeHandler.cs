using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FishingWithGit;

namespace HarmonizeGit
{
    public class PostTakeHandler : TypicalHandlerBase
    {
        TakeArgs args;
        public override IGitHookArgs Args => args;

        public PostTakeHandler(HarmonizeGitBase harmonize, TakeArgs args)
            : base(harmonize)
        {
            this.args = args;
        }

        public override async Task<bool> Handle()
        {
            return this.harmonize.SyncParentRepos();
        }
    }
}
