using FishingWithGit;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                this.harmonize.WriteLine("Target commit was the same as the source commit.");
                return true;
            }

            if (this.harmonize.CancelIfParentsHaveChanges())
            {
                return false;
            }
            
            harmonize.SyncParentReposToSha(args.TargetSha);
            return true;
        }
    }
}
