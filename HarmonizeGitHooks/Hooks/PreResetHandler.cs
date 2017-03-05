using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGitHooks
{
    class PreResetHandler : TypicalHandlerBase
    {
        public PreResetHandler(HarmonizeGitBase harmonize) 
            : base(harmonize)
        {
        }

        public override async Task<bool> Handle(List<string> args)
        {
            await this.harmonize.ChildLoader.RemoveCurrentConfig();
            return true;
        }
    }
}
