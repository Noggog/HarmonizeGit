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
    public class Status_Tests
    {
        public ConfigCheckout GetPreppedCheckout()
        {
            var checkout = Repository_Tools.GetStandardConfigCheckout();
            Commands.Checkout(checkout.ParentRepo.Repo, checkout.Parent_SecondSha);
            return checkout;
        }

        [Fact]
        public async Task BreakOutIfInOperation()
        {
            using (var checkout = GetPreppedCheckout())
            {
                await checkout.Init();
                Commands.Checkout(checkout.ParentRepo.Repo, checkout.Parent_SecondSha);
                var backBranch = checkout.Repo.Repo.CreateBranch("BackBranch", checkout.Child_SecondSha);
                Commands.Checkout(checkout.Repo.Repo, backBranch);
                var filePath = Path.Combine(checkout.Repo.Dir.FullName, Utility.STANDARD_FILE);
                File.WriteAllText(filePath, "Dirty\n");
                Commands.Stage(checkout.Repo.Repo, filePath);
                checkout.Repo.Repo.Commit("Breakoff commit", Utility.GetSignature(), Utility.GetSignature());
                var mergeResult = checkout.Repo.Repo.Merge(checkout.Child_ThirdSha, Utility.GetSignature());
                Assert.Equal(MergeStatus.Conflicts, mergeResult.Status);

                StatusHandler handler = new StatusHandler(checkout.Harmonize, new StatusArgs());
                var result = await handler.Handle();
                Assert.True(result);
                var configStatus = checkout.Repo.Repo.RetrieveStatus(HarmonizeGitBase.HarmonizeConfigPath);
                Assert.Equal(FileStatus.Unaltered, configStatus);
            }
        }

        [Fact]
        public async Task SyncConfig()
        {
            using (var checkout = GetPreppedCheckout())
            {
                await checkout.Init();
                Commands.Checkout(checkout.ParentRepo.Repo, checkout.Parent_SecondSha);
                StatusHandler handler = new StatusHandler(checkout.Harmonize, new StatusArgs());
                var result = await handler.Handle();
                Assert.True(result);
                var configStatus = checkout.Repo.Repo.RetrieveStatus(HarmonizeGitBase.HarmonizeConfigPath);
                Assert.Equal(FileStatus.ModifiedInWorkdir, configStatus);
            }
        }
    }
}
