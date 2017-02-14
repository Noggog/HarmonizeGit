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
        string curSha;
        string targetSha;

        public CheckoutHandler(HarmonizeGitBase harmonize)
            : base(harmonize)
        {
        }

        private void ParseArgs(List<string> args)
        {
            if (args.Count < 2)
            {
                throw new ArgumentException("Checkout args were shorter than expected: " + args.Count);
            }

            this.curSha = args[0];
            this.targetSha = args[1];
            if (curSha.Length != 40)
            {
                throw new ArgumentException("Checkout args for current sha was shorter than expected: " + curSha.Length);
            }
            if (targetSha.Length != 40)
            {
                throw new ArgumentException("Checkout args for target sha was shorter than expected: " + targetSha.Length);
            }
        }

        public override bool Handle(List<string> args)
        {
            ParseArgs(args);

            // If moving to the same commit, just exit
            if (curSha.Equals(targetSha))
            {
                this.harmonize.WriteLine("Target commit was the same as the source commit.");
                return true;
            }

            var uncomittedChangeRepos = this.harmonize.GetReposWithUncommittedChanges();
            if (uncomittedChangeRepos.Count > 0)
            {
                this.harmonize.WriteLine("Cancelling because repos had uncommitted changes:");
                foreach (var repo in uncomittedChangeRepos)
                {
                    this.harmonize.WriteLine("   -" + repo.Nickname);
                }
                return false;
            }

            harmonize.SyncParentReposToSha(targetSha);
            return true;
        }
    }
}
