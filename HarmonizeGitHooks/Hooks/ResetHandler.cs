using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGitHooks
{
    class ResetHandler : TypicalHandlerBase
    {
        public ResetHandler(HarmonizeGitBase harmonize) 
            : base(harmonize)
        {
        }

        public override async Task<bool> Handle(List<string> args)
        {
            harmonize.SyncParentRepos();
            return true;
        }
    }
}
