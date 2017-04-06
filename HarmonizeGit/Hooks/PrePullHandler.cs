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
            using (var repo = new Repository(this.harmonize.TargetPath))
            {
                var configStatus = repo.RetrieveStatus(HarmonizeGitBase.HarmonizeConfigPath);
                if (configStatus == FileStatus.Unaltered) return true;
                repo.CheckoutPaths(
                    repo.Head.Tip.Sha,
                    new string[] { HarmonizeGitBase.HarmonizeConfigPath },
                    new CheckoutOptions()
                    {
                        CheckoutModifiers = CheckoutModifiers.Force,
                    });
                return true;
            }
        }
    }
}
