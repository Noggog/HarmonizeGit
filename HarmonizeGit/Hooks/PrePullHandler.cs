using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGit
{
    public class PrePullHandler : TypicalHandlerBase
    {
        public PrePullHandler(HarmonizeGitBase harmonize)
            : base(harmonize)
        {
        }

        public override async Task<bool> Handle(string[] args)
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
