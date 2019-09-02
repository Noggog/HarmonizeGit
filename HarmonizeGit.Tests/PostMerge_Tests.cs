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
    public class PostMerge_Tests
    {
        [Fact]
        public async Task InsertCurrentConfig()
        {
            using (var checkout = Repository_Tools.GetStandardConfigCheckout())
            {
                await checkout.Init();
                var parentCommit = checkout.ParentRepo.Repo.Lookup<Commit>(checkout.Parent_SecondSha);
                File.WriteAllText(checkout.Repo.Repo.Info.WorkingDirectory + Utility.STANDARD_FILE, "Dirty");
                Commands.Stage(checkout.Repo.Repo, Utility.STANDARD_FILE);
                var commit = checkout.Repo.Repo.Commit(
                    "A Commit",
                    Utility.GetSignature(),
                    Utility.GetSignature());
                var handler = new PostMergeHandler(checkout.Harmonize, new MergeArgs());
                await handler.Handle();
                var childGet = await checkout.ParentHarmonize.ChildLoader.LookupChildUsage(commit.Sha);
                Assert.True(childGet.Succeeded);
                Assert.Equal(commit.Sha, childGet.Value.Sha);
                Assert.Equal(checkout.Repo.Dir.FullName, childGet.Value.ChildRepoPath);
                Assert.Equal(checkout.ParentRepo.Repo.Head.Tip.Sha, childGet.Value.ParentSha);
                Assert.Equal(checkout.ParentRepo.Dir.FullName, childGet.Value.ParentRepoPath);
            }
        }

        [Fact]
        public async Task ParentSyncedToTarget()
        {
            using (var checkout = Repository_Tools.GetStandardConfigCheckout())
            {
                Commands.Checkout(checkout.Repo.Repo, checkout.Child_SecondSha, new CheckoutOptions() { CheckoutModifiers = CheckoutModifiers.Force });
                await checkout.Init();
                PostMergeHandler handler = new PostMergeHandler(checkout.Harmonize, new MergeArgs());
                Settings.Instance.MovingSyncsParents = true;
                var ret = await handler.Handle();
                Assert.True(ret);
                Assert.Equal(checkout.Parent_SecondSha, checkout.ParentRepo.Repo.Head.Tip.Sha);
            }
        }
    }
}
