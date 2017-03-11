using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGitHooks
{
    class PostRebaseAbortHandler : TypicalHandlerBase
    {
        public PostRebaseAbortHandler(HarmonizeGitBase harmonize)
            : base(harmonize)
        {
            this.NeedsConfig = false;
        }

        public override async Task<bool> Handle(List<string> args)
        {
            // ToDo
            // Needs to reinsert commits to parent
            throw new ArgumentException("Needs to reinsert commits to parent");
        }
    }
}
