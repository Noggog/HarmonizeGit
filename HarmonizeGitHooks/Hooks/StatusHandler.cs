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

        public override bool Handle(List<string> args)
        {
            using (var repo = new Repository("."))
            {
                if (repo.Info.CurrentOperation != CurrentOperation.None) return true;
            }
            try
            {
                this.harmonize.SyncConfigToParentShas();
            }
            catch (Exception ex)
            {
                this.harmonize.WriteLine("Failed up sync config. " + ex.Message);
            }
            return true;
        }
    }
}
