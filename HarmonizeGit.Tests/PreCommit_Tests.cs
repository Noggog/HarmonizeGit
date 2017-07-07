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
    public class PreCommit_Tests
    {

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
                CommitArgs args = new CommitArgs();
                PreCommitHandler handler = new PreCommitHandler(checkout.Harmonize, args);
                var ret = await handler.Handle();
                Assert.True(ret);
                Assert.False(checkout.ParentRepo.Repo.RetrieveStatus().IsDirty);
            }
        }

        [Fact]
        public async Task DirtyParent()
        {
            using (var checkout = Repository_Tools.GetStandardConfigCheckout())
            {
                await checkout.Init();
                File.WriteAllText(checkout.ParentRepo.Repo.Info.WorkingDirectory + Utility.STANDARD_FILE, "Prep");
                CommitArgs args = new CommitArgs();
                PreCommitHandler handler = new PreCommitHandler(checkout.Harmonize, args);
                var ret = await handler.Handle();
                Assert.False(ret);
            }
        }

        [Fact]
        public async Task SyncConfigToParent()
        {
            using (var checkout = Repository_Tools.GetStandardConfigCheckout())
            {
                await checkout.Init();
                Commands.Checkout(checkout.ParentRepo.Repo, checkout.Parent_SecondSha);
                CommitArgs args = new CommitArgs();
                PreCommitHandler handler = new PreCommitHandler(checkout.Harmonize, args);
                var ret = await handler.Handle();
                Assert.True(ret);
                HarmonizeConfig config = checkout.Harmonize.ConfigLoader.GetConfig(
                    checkout.Repo.Dir.FullName,
                    force: true);
                var parentListing = config.ParentRepos.Where((l) => l.Path.Equals(checkout.ParentRepo.Dir.FullName)).First();
                Assert.Equal(checkout.Parent_SecondSha, parentListing.Sha);
            }
        }

        [Fact]
        public async Task StageHarmonizeConfig()
        {
            using (var checkout = Repository_Tools.GetStandardConfigCheckout())
            {
                await checkout.Init();

                var stat = checkout.Repo.Repo.RetrieveStatus(HarmonizeGitBase.HarmonizeConfigPath);
                Assert.Equal(FileStatus.Unaltered, stat);

                var parentCommit = checkout.ParentRepo.Repo.Lookup<Commit>(checkout.Parent_SecondSha);
                checkout.ChildToParentListing.SetToCommit(parentCommit);
                checkout.Harmonize.Config.WriteToPath(checkout.Repo.Repo.Info.WorkingDirectory + HarmonizeGitBase.HarmonizeConfigPath);
                Assert.True(checkout.Repo.Repo.RetrieveStatus().IsDirty);

                stat = checkout.Repo.Repo.RetrieveStatus(HarmonizeGitBase.HarmonizeConfigPath);
                Assert.Equal(FileStatus.ModifiedInWorkdir, stat);
                CommitArgs args = new CommitArgs();
                PreCommitHandler handler = new PreCommitHandler(checkout.Harmonize, args);
                var ret = await handler.Handle();
                Assert.True(ret);
                stat = checkout.Repo.Repo.RetrieveStatus(HarmonizeGitBase.HarmonizeConfigPath);
                Assert.Equal(FileStatus.ModifiedInIndex, stat);
            }
        }

        [Fact]
        public async Task Amending_BlockIfChildrenAreUsing()
        {
            using (var checkout = Repository_Tools.GetStandardConfigCheckout())
            {
                await checkout.Init();

                CommitArgs args = new CommitArgs()
                {
                    Amending = true
                };
                PreCommitHandler handler = new PreCommitHandler(checkout.ParentHarmonize, args);
                var ret = await handler.Handle();
                Assert.False(ret);
            }
        }

        [Fact]
        public async Task Amending_RemoveUsagesFromParent()
        {
            using (var checkout = Repository_Tools.GetStandardConfigCheckout())
            {
                await checkout.Init();

                CommitArgs args = new CommitArgs()
                {
                    Amending = true
                };
                PreCommitHandler handler = new PreCommitHandler(checkout.Harmonize, args);
                var ret = await handler.Handle();
                Assert.True(ret);
                var usage = await checkout.Harmonize.ChildLoader.LookupChildUsage(checkout.Child_FourthSha);
                Assert.False(usage.Succeeded);
            }
        }
    }
}
