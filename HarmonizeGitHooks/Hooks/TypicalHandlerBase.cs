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
        public bool Silent { get; protected set; }
        public bool NeedsConfig { get; protected set; } = true;

        public TypicalHandlerBase(HarmonizeGitBase harmonize)
        {
            this.harmonize = harmonize;
        }

        public abstract Task<bool> Handle(List<string> args);
    }
}
