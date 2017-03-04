using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGitHooks
{
    class TakeHandler : TypicalHandlerBase
    {
        public TakeHandler(HarmonizeGitBase harmonize)
            : base(harmonize)
        {
        }

        public override async Task<bool> Handle(List<string> args)
        {
            this.harmonize.SyncParentRepos();
            return true;
        }
    }
}
