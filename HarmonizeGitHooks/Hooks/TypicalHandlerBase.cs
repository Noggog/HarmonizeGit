using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGitHooks
{
    abstract class TypicalHandlerBase
    {
        protected HarmonizeGitBase harmonize;

        public TypicalHandlerBase(HarmonizeGitBase harmonize)
        {
            this.harmonize = harmonize;
        }

        public abstract void Handle(List<string> args);
    }
}
