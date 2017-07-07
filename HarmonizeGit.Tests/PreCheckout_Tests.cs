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
    public class PreCheckout_Tests
    {
        [Fact]
        public async Task DirtyConfig()
        {
            using (var checkout = Repository_Tools.GetStandardConfigCheckout())
            {
                await checkout.Init();
                var parentCommit = checkout.ParentRepo.Repo.Lookup<Commit>(checkout.Parent_SecondSha);
                checkout.Harmonize.Config.ParentRepos[0].SetToCommit(parentCommit);
                checkout.Harmonize.Config.WriteToPath(checkout.Repo.Repo.Info.WorkingDirectory + HarmonizeGitBase.HarmonizeConfigPath);
                Assert.True(checkout.Repo.Repo.RetrieveStatus().IsDirty);
                CheckoutArgs args = new CheckoutArgs()
                {
                    CurrentSha = checkout.Repo.Repo.Head.Tip.Sha,
                    TargetSha = checkout.Child_SecondSha
                };
                PreCheckoutHandler handler = new PreCheckoutHandler(checkout.Harmonize, args);
                var ret = await handler.Handle();
                Assert.True(ret);
            }
        }

        [Fact]
        public async Task DirtyParentConfig()
        {
            using (var checkout = Repository_Tools.GetStandardConfigCheckout())
            {
                await checkout.Init();
                var superParentCommit = checkout.SuperParentRepo.Repo.Lookup<Commit>(checkout.SuperParent_FirstSha);
                checkout.ParentHarmonize.Config.ParentRepos[0].SetToCommit(superParentCommit);
                checkout.ParentHarmonize.Config.WriteToPath(checkout.ParentRepo.Repo.Info.WorkingDirectory + HarmonizeGitBase.HarmonizeConfigPath);
                Assert.True(checkout.ParentRepo.Repo.RetrieveStatus().IsDirty);
                CheckoutArgs args = new CheckoutArgs()
                {
                    CurrentSha = checkout.Repo.Repo.Head.Tip.Sha,
                    TargetSha = checkout.Child_SecondSha
                };
                PreCheckoutHandler handler = new PreCheckoutHandler(checkout.Harmonize, args);
                var ret = await handler.Handle();
                Assert.True(ret);
                Assert.False(checkout.ParentRepo.Repo.RetrieveStatus().IsDirty);
            }
        }

        [Fact]
        public async Task SuperParentUntrackedCommit()
        {
            using (var checkout = Repository_Tools.GetStandardConfigCheckout())
            {
                await checkout.Init();
                File.WriteAllText(checkout.SuperParentFile.FullName, "DirtyContent\n");
                Commands.Stage(checkout.SuperParentRepo.Repo, checkout.SuperParentFile.FullName);
                checkout.SuperParentRepo.Repo.Commit("New commit", Utility.GetSignature(), Utility.GetSignature());
                checkout.ParentHarmonize.SyncConfigToParentShas();
                checkout.Harmonize.SyncConfigToParentShas();
                Assert.False(checkout.SuperParentRepo.Repo.RetrieveStatus().IsDirty);
                Assert.True(checkout.ParentRepo.Repo.RetrieveStatus().IsDirty);
                Assert.True(checkout.Repo.Repo.RetrieveStatus().IsDirty);
                CheckoutArgs args = new CheckoutArgs()
                {
                    CurrentSha = checkout.Repo.Repo.Head.Tip.Sha,
                    TargetSha = checkout.Child_SecondSha
                };
                PreCheckoutHandler handler = new PreCheckoutHandler(checkout.Harmonize, args);
                var ret = await handler.Handle();
                Assert.True(ret);
                Assert.False(checkout.ParentRepo.Repo.RetrieveStatus().IsDirty);
                Assert.False(checkout.Repo.Repo.RetrieveStatus().IsDirty);
                Assert.False(checkout.SuperParentRepo.Repo.RetrieveStatus().IsDirty);
            }
        }

        [Fact]
        public async Task DirtyParent()
        {
            using (var checkout = Repository_Tools.GetStandardConfigCheckout())
            {
                await checkout.Init();
                File.WriteAllText(checkout.ParentRepo.Repo.Info.WorkingDirectory + Utility.STANDARD_FILE, "Prep");
                CheckoutArgs args = new CheckoutArgs()
                {
                    CurrentSha = checkout.Repo.Repo.Head.Tip.Sha,
                    TargetSha = checkout.Child_SecondSha
                };
                PreCheckoutHandler handler = new PreCheckoutHandler(checkout.Harmonize, args);
                var ret = await handler.Handle();
                Assert.False(ret);
            }
        }

        [Fact]
        public async Task ParentSyncedToTarget()
        {
            using (var checkout = Repository_Tools.GetStandardConfigCheckout())
            {
                await checkout.Init();
                CheckoutArgs args = new CheckoutArgs()
                {
                    CurrentSha = checkout.Repo.Repo.Head.Tip.Sha,
                    TargetSha = checkout.Child_SecondSha
                };
                PreCheckoutHandler handler = new PreCheckoutHandler(checkout.Harmonize, args);
                var ret = await handler.Handle();
                Assert.True(ret);
                Assert.Equal(checkout.Parent_SecondSha, checkout.ParentRepo.Repo.Head.Tip.Sha);
            }
        }
    }
}
