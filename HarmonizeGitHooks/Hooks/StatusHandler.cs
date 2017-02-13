using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGitHooks
{
    class StatusHandler : TypicalHandlerBase
    {
        public StatusHandler(HarmonizeGitBase harmonize)
            : base(harmonize)
        {
        }

        public override void Handle(List<string> args)
        {
            try
            {
                this.harmonize.SyncConfigToParentShas();
            }
            catch (Exception ex)
            {
                this.harmonize.WriteLine("Failed up sync config. " + ex.Message);
            }
        }
    }
}
