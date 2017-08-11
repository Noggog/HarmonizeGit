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
            if (!CheckRepoStatus()) return false;
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

        public bool CheckRepoStatus()
        {
            switch (this.harmonize.Repo.Info.CurrentOperation)
            {
                case CurrentOperation.None:
                    return true;
                case CurrentOperation.Merge:
                    return !this.harmonize.Repo.RetrieveStatus(
                        HarmonizeGitBase.HarmonizeConfigPath)
                        .HasFlag(FileStatus.Conflicted);
                default:
                    return false;
            }
        }
    }
}
