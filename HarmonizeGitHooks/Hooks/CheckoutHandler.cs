using FishingWithGit;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGitHooks
{
    class CheckoutHandler : TypicalHandlerBase
    {
        public CheckoutHandler(HarmonizeGitBase harmonize)
            : base(harmonize)
        {
        }

        public override async Task<bool> Handle(string[] args)
        {
            var checkoutArgs = new CheckoutArgs(args);

            // If moving to the same commit, just exit
            if (checkoutArgs.CurrentSha.Equals(checkoutArgs.TargetSha))
            {
                this.harmonize.WriteLine("Target commit was the same as the source commit.");
                return true;
            }

            if (this.harmonize.IsDirty())
            {
                this.harmonize.WriteLine("Current working directory has uncommitted changes.");
                return false;
            }

            if (this.harmonize.CancelIfParentsHaveChanges())
            {
                return false;
            }
            
            harmonize.SyncParentReposToSha(checkoutArgs.TargetSha);
            return true;
        }
    }
}
