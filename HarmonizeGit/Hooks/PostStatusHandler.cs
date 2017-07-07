using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FishingWithGit;

namespace HarmonizeGit
{
    public class PostStatusHandler : TypicalHandlerBase
    {
        StatusArgs args;
        public override IGitHookArgs Args => args;

        public PostStatusHandler(HarmonizeGitBase harmonize, StatusArgs args)
            : base(harmonize)
        {
            this.args = args;
        }

        public override async Task<bool> Handle()
        {
            using (var repo = new Repository(this.harmonize.TargetPath))
            {
                if (repo.Info.CurrentOperation != CurrentOperation.None) return true;
            }
            try
            {
                await this.harmonize.SyncConfigToParentShas();
            }
            catch (Exception ex)
            {
                this.harmonize.WriteLine("Failed up sync config. " + ex);
            }
            return true;
        }
    }
}
