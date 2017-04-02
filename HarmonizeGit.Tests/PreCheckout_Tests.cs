using FishingWithGit;
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
    public class PreCheckout_Tests
    {
        [Fact]
        public async Task DirtyConfig()
        {
            using (var checkout = Repository_Tools.GetStandardConfigCheckout())
            {
                checkout.Init();
                var parentCommit = checkout.ParentRepo.Repo.Lookup<Commit>(checkout.Parent_SecondSha);
                checkout.Config.ParentRepos[0].SetToCommit(parentCommit);
                File.WriteAllText(checkout.Repo.Repo.Info.WorkingDirectory + HarmonizeGitBase.HarmonizeConfigPath, checkout.Config.GetXmlStr());
                Assert.True(checkout.Repo.Repo.RetrieveStatus().IsDirty);
                CheckoutArgs args = new CheckoutArgs()
                {
                    CurrentSha = checkout.Repo.Repo.Head.Tip.Sha,
                    TargetSha = checkout.Child_SecondSha
                };
                PreCheckoutHandler handler = new PreCheckoutHandler(checkout.Harmonize);
                var ret = await handler.Handle(args.ToArray());
                Assert.True(ret);
            }
        }

        [Fact]
        public void DirtyParentConfig()
        {
            Assert.False(true);
        }

        [Fact]
        public async Task Dirty()
        {
            using (var checkout = Repository_Tools.GetStandardConfigCheckout())
            {
                checkout.Init();
                File.WriteAllText(checkout.ParentRepo.Repo.Info.WorkingDirectory + Repository_Tools.STANDARD_FILE, "Prep");
                CheckoutArgs args = new CheckoutArgs()
                {
                    CurrentSha = checkout.Repo.Repo.Head.Tip.Sha,
                    TargetSha = checkout.Child_SecondSha
                };
                PreCheckoutHandler handler = new PreCheckoutHandler(checkout.Harmonize);
                var ret = await handler.Handle(args.ToArray());
                Assert.False(ret);
            }
        }

        [Fact]
        public async Task ParentSyncedToTarget()
        {
            using (var checkout = Repository_Tools.GetStandardConfigCheckout())
            {
                checkout.Init();
                CheckoutArgs args = new CheckoutArgs()
                {
                    CurrentSha = checkout.Repo.Repo.Head.Tip.Sha,
                    TargetSha = checkout.Child_SecondSha
                };
                PreCheckoutHandler handler = new PreCheckoutHandler(checkout.Harmonize);
                var ret = await handler.Handle(args.ToArray());
                Assert.True(ret);
                Assert.Equal(checkout.Parent_SecondSha, checkout.ParentRepo.Repo.Head.Tip.Sha);
            }
        }
    }
}
