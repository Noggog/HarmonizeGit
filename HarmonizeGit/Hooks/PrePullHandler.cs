using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FishingWithGit;

namespace HarmonizeGit
{
    public class PrePullHandler : TypicalHandlerBase
    {
        PullArgs args;
        public override IGitHookArgs Args => args;

        public PrePullHandler(HarmonizeGitBase harmonize, PullArgs args)
            : base(harmonize)
        {
            this.args = args;
        }

        public override async Task<bool> Handle()
        {
            var repo = this.harmonize.Repo;
            var configStatus = repo.RetrieveStatus(Constants.HarmonizeConfigPath);
            if (configStatus == FileStatus.Unaltered)
            {
                this.harmonize.Logger.WriteLine("Harmonize config unaltered. Continuing pull.");
                return true;
            }
            this.harmonize.Logger.WriteLine("Purging modified harmonize config to allow pull.");
            repo.CheckoutPaths(
                repo.Head.Tip.Sha,
                new string[] { Constants.HarmonizeConfigPath },
                new CheckoutOptions()
                {
                    CheckoutModifiers = CheckoutModifiers.Force,
                });
            return true;
        }
    }
}
