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

        public TypicalHandlerBase(HarmonizeGitBase harmonize)
        {
            this.harmonize = harmonize;
        }

        public abstract bool Handle(List<string> args);
    }
}
