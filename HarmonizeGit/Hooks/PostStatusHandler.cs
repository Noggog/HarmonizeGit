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
            var repo = this.harmonize.Repo;
            if (repo.Info.CurrentOperation != CurrentOperation.None) return true;
            try
            {
                await this.harmonize.SyncConfigToParentShas();
            }
            catch (Exception ex)
            {
                this.harmonize.Logger.WriteLine("Failed up sync config. " + ex, error: true);
            }
            return true;
        }
    }
}
