using FishingWithGit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGit
{
    public class PostMergeHandler : TypicalHandlerBase
    {
        MergeArgs args;
        public override IGitHookArgs Args => args;

        public PostMergeHandler(HarmonizeGitBase harmonize, MergeArgs args)
            : base(harmonize)
        {
            this.args = args;
        }

        public override async Task<bool> Handle()
        {
            harmonize.SyncParentRepos();
            await this.harmonize.ChildLoader.InsertCurrentConfig();
            return true;
        }
    }
}
