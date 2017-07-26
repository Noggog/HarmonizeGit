using FishingWithGit.Tests.Common;
using HarmonizeGit;
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
    public class HarmonizeGit_Tests
    {
        public HarmonizeGitBase GetHarmonize(string path)
        {
            HarmonizeGitBase gitBase = new HarmonizeGitBase(path);
            return gitBase;
        }

        #region IsDirty
        [Fact]
        public async Task IsDirty_Normal()
        {
            using (var repo = Repository_Tools.GetStandardRepo())
            {
                File.WriteAllText(
                    Path.Combine(repo.Repo.Info.WorkingDirectory, Utility.STANDARD_FILE),
                    "dirty work");
                HarmonizeGitBase gitBase = GetHarmonize(repo.Repo.Info.WorkingDirectory);
                Assert.True((await gitBase.IsDirty(configExclusion: ConfigExclusion.None, regenerateConfig: false)).Succeeded);
            }
        }

        [Fact]
        public async Task IsDirty_NotDirty()
        {
            using (var repo = Repository_Tools.GetStandardRepo())
            {
                HarmonizeGitBase gitBase = GetHarmonize(repo.Repo.Info.WorkingDirectory);
                Assert.False((await gitBase.IsDirty(configExclusion: ConfigExclusion.None, regenerateConfig: false)).Succeeded);
            }
        }

        [Fact]
        public async Task IsDirty_WithConfig()
        {
            using (var repo = Repository_Tools.GetStandardRepo())
            {
                File.WriteAllText(
                    Path.Combine(repo.Repo.Info.WorkingDirectory, Utility.STANDARD_FILE),
                    "dirty work");
                File.WriteAllText(
                    Path.Combine(repo.Repo.Info.WorkingDirectory, HarmonizeGitBase.HarmonizeConfigPath),
                    "dirty work");
                HarmonizeGitBase gitBase = GetHarmonize(repo.Repo.Info.WorkingDirectory);
                Assert.True((await gitBase.IsDirty(configExclusion: ConfigExclusion.Full, regenerateConfig: false)).Succeeded);
            }
        }

        [Fact]
        public async Task IsDirty_OnlyConfig_Include()
        {
            using (var repo = Repository_Tools.GetStandardRepo())
            {
                File.WriteAllText(
                    Path.Combine(repo.Repo.Info.WorkingDirectory, HarmonizeGitBase.HarmonizeConfigPath),
                    "dirty work");
                HarmonizeGitBase gitBase = GetHarmonize(repo.Repo.Info.WorkingDirectory);
                Assert.True((await gitBase.IsDirty(configExclusion: ConfigExclusion.None, regenerateConfig: false)).Succeeded);
            }
        }

        [Fact]
        public async Task IsDirty_OnlyConfig_Exclude()
        {
            using (var repo = Repository_Tools.GetStandardRepo())
            {
                File.WriteAllText(
                    Path.Combine(repo.Repo.Info.WorkingDirectory, HarmonizeGitBase.HarmonizeConfigPath),
                    "dirty work");
                HarmonizeGitBase gitBase = GetHarmonize(repo.Repo.Info.WorkingDirectory);
                Assert.False((await gitBase.IsDirty(configExclusion: ConfigExclusion.Full, regenerateConfig: false)).Succeeded);
            }
        }
        #endregion

        #region GetReposWithUncommittedChanges
        [Fact]
        public async Task GetReposWithUncommittedChanges_NoChanges()
        {
            using (var checkout = Repository_Tools.GetStandardConfigCheckout())
            {
                await checkout.Init();
                Assert.Empty(await checkout.Harmonize.GetReposWithUncommittedChanges());
            }
        }

        [Fact]
        public async Task GetReposWithUncommittedChanges()
        {
            using (var checkout = Repository_Tools.GetStandardConfigCheckout())
            {
                await checkout.Init();
                var parentRepo = checkout.ParentRepo;
                File.WriteAllText(
                    Path.Combine(parentRepo.Dir.FullName, Utility.STANDARD_FILE),
                    "Dirty changes");
                var changes = await checkout.Harmonize.GetReposWithUncommittedChanges();
                Assert.Equal(1, changes.Count);
                Assert.Equal(changes[0].Item1.Path, parentRepo.Dir.FullName);
            }
        }

        [Fact]
        public async Task GetReposWithUncommittedChanges_JustConfig()
        {
            using (var checkout = Repository_Tools.GetStandardConfigCheckout())
            {
                await checkout.Init();
                var superParentCommit = checkout.SuperParentRepo.Repo.Lookup<Commit>(checkout.SuperParent_FirstSha);
                checkout.ParentHarmonize.Config.ParentRepos[0].SetToCommit(superParentCommit);
                checkout.ParentHarmonize.Config.WriteToPath(checkout.ParentRepo.Repo.Info.WorkingDirectory + HarmonizeGitBase.HarmonizeConfigPath);
                Assert.True(checkout.ParentRepo.Repo.RetrieveStatus().IsDirty);
                var changes = await checkout.Harmonize.GetReposWithUncommittedChanges();
                Assert.Equal(0, changes.Count);
            }
        }
        #endregion

        #region SyncParentReposToConfig
        [Fact]
        public async Task SyncParentReposToConfig_Typical()
        {
            using (var checkout = Repository_Tools.GetStandardConfigCheckout())
            {
                checkout.Repo.Repo.Checkout(checkout.Child_FirstSha);
                await checkout.Init();

                Assert.True(checkout.Harmonize.SyncParentRepos());
                Assert.Equal(checkout.Parent_FirstSha, checkout.ParentRepo.Repo.Head.Tip.Sha);
                Assert.Equal(checkout.SuperParent_SecondSha, checkout.SuperParentRepo.Repo.Head.Tip.Sha);
            }
        }
        #endregion

        #region Insert Usages
        [Fact]
        public async Task Typical_Seed()
        {
            using (var checkout = Repository_Tools.GetStandardConfigCheckout())
            {
                await checkout.Init();

                await checkout.Harmonize.ChildLoader.InitializeIntoParents();
                FileInfo dbFile = new FileInfo(Path.Combine(checkout.ParentRepo.Dir.FullName, HarmonizeGitBase.HarmonizeChildrenDBPath));
                Assert.True(dbFile.Exists);
                dbFile = new FileInfo(Path.Combine(checkout.SuperParentRepo.Dir.FullName, HarmonizeGitBase.HarmonizeChildrenDBPath));
                Assert.True(dbFile.Exists);
            }
        }

        [Fact]
        public async Task Relative_Seed()
        {
            using (var checkout = Repository_Tools.GetStandardConfigCheckout())
            {
                await checkout.Init();

                await checkout.Harmonize.ChildLoader.InitializeIntoParents();
                FileInfo dbFile = new FileInfo(Path.Combine(checkout.ParentRepo.Dir.FullName, HarmonizeGitBase.HarmonizeChildrenDBPath));
                Assert.True(dbFile.Exists);
                dbFile = new FileInfo(Path.Combine(checkout.SuperParentRepo.Dir.FullName, HarmonizeGitBase.HarmonizeChildrenDBPath));
                Assert.True(dbFile.Exists);
            }
        }
        #endregion
    }
}
