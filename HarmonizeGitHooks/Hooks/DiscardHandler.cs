using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGitHooks
{
    class DiscardHandler : TypicalHandlerBase
    {
        public DiscardHandler(HarmonizeGitBase harmonize)
            : base(harmonize)
        {
        }

        public override bool Handle(List<string> args)
        {
            this.harmonize.SyncParentRepos();
            return true;
        }
    }
}
