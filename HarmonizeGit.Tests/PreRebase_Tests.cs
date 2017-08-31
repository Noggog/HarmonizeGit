using FishingWithGit;
using FishingWithGit.Tests.Common;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace HarmonizeGit.Tests
{
    public class PreRebase_Tests
    {
        public string AncestorSha;
        public string OldSha;
        public string FillerSha;

        public Branch TargetBranch;
        public Branch AncestorBranch;

        public const string TARGET_BRANCH_NAME = "TargetBranch";

        public async Task<ConfigCheckout> GetCheckout()
        {
            var checkout = Repository_Tools.GetStandardConfigCheckout();
            await checkout.Init();
            var parentCommit = checkout.ParentRepo.Repo.Lookup<Commit>(checkout.Parent_SecondSha);
            this.AncestorSha = checkout.Repo.Repo.Head.Tip.Sha;
            var signature = Utility.GetSignature();
            this.AncestorBranch = checkout.Repo.Repo.CreateBranch("AncestorBranch");

            File.WriteAllText(checkout.Repo.Repo.Info.WorkingDirectory + Utility.STANDARD_FILE, "Prep");
            Commands.Stage(checkout.Repo.Repo, Utility.STANDARD_FILE);
            var fillerCommit = checkout.Repo.Repo.Commit(
                "I'm just a commit",
                signature,
                signature);
            this.FillerSha = fillerCommit.Sha;
            this.TargetBranch = checkout.Repo.Repo.CreateBranch(TARGET_BRANCH_NAME);

            Commands.Checkout(checkout.Repo.Repo, this.AncestorBranch, new CheckoutOptions() { CheckoutModifiers = CheckoutModifiers.Force });
            File.WriteAllText(checkout.Repo.Repo.Info.WorkingDirectory + Utility.STANDARD_FILE, "Dirty");
            Commands.Stage(checkout.Repo.Repo, Utility.STANDARD_FILE);
            var oldCommit = checkout.Repo.Repo.Commit(
                "Commit to rebase",
                signature,
                signature);
            this.OldSha = oldCommit.Sha;
            return checkout;
        }

        [Fact]
        public async Task BadBranch()
        {
            using (var checkout = await this.GetCheckout())
            {
                await checkout.Init();
                RebaseArgs args = new RebaseArgs()
                {
                    Target = "Nothing"
                };
                PreRebaseHandler handler = new PreRebaseHandler(checkout.Harmonize, args);
                var ret = await handler.Handle();
                Assert.False(ret);
            }
        }

        [Fact]
        public async Task Typical()
        {
            using (var checkout = await this.GetCheckout())
            {
                await checkout.Init();
                RebaseArgs args = new RebaseArgs()
                {
                    Target = TARGET_BRANCH_NAME
                };
                PreRebaseHandler handler = new PreRebaseHandler(checkout.Harmonize, args);
                var ret = await handler.Handle();
                Assert.True(ret);
            }
        }

        [Fact]
        public async Task BlockIfChildrenUsing()
        {
            using (var checkout = Repository_Tools.GetStandardConfigCheckout())
            {
                await checkout.Init();
                var targetBranch = checkout.ParentRepo.Repo.CreateBranch(TARGET_BRANCH_NAME, checkout.Parent_SecondSha);
                RebaseArgs args = new RebaseArgs()
                {
                    Target = TARGET_BRANCH_NAME
                };
                PreRebaseHandler handler = new PreRebaseHandler(checkout.ParentHarmonize, args);
                var ret = await handler.Handle();
                Assert.False(ret);
            }
        }
    }
}
