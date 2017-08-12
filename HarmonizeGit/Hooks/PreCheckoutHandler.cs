using FishingWithGit;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HarmonizeGit
{
    public class PreCheckoutHandler : TypicalHandlerBase
    {
        CheckoutArgs args;
        public override IGitHookArgs Args => args;

        public PreCheckoutHandler(HarmonizeGitBase harmonize, CheckoutArgs args)
            : base(harmonize)
        {
            this.args = args;
        }

        public override async Task<bool> Handle()
        {
            // If moving to the same commit, just exit
            if (args.CurrentSha.Equals(args.TargetSha))
            {
                this.harmonize.Logger.WriteLine("Target commit was the same as the source commit.");
                return true;
            }

            if (await this.harmonize.CancelIfParentsHaveChanges())
            {
                return false;
            }
            
            this.harmonize.Repo.Discard(Path.Combine(harmonize.TargetPath, Constants.HarmonizeConfigPath));
            return harmonize.SyncParentReposToSha(args.TargetSha);
        }
    }
}
