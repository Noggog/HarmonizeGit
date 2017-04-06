using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FishingWithGit;

namespace HarmonizeGit
{
    public class PostCommitHandler : TypicalHandlerBase
    {
        CommitArgs args;
        public override IGitHookArgs Args => args;

        public PostCommitHandler(HarmonizeGitBase harmonize, CommitArgs args)
            : base(harmonize)
        {
            this.args = args;
        }

        public override async Task<bool> Handle()
        {
            await this.harmonize.ChildLoader.InsertCurrentConfig();
            return true;
        }
    }
}
