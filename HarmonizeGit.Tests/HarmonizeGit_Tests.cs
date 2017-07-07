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
        public void IsDirty_Normal()
        {
            using (var repo = Repository_Tools.GetStandardRepo())
            {
                File.WriteAllText(
                    Path.Combine(repo.Repo.Info.WorkingDirectory, Utility.STANDARD_FILE),
                    "dirty work");
                HarmonizeGitBase gitBase = GetHarmonize(repo.Repo.Info.WorkingDirectory);
                Assert.True(gitBase.IsDirty(excludeHarmonizeConfig: false, regenerateConfig: false));
            }
        }

        [Fact]
        public void IsDirty_NotDirty()
        {
            using (var repo = Repository_Tools.GetStandardRepo())
            {
                HarmonizeGitBase gitBase = GetHarmonize(repo.Repo.Info.WorkingDirectory);
                Assert.False(gitBase.IsDirty(excludeHarmonizeConfig: false, regenerateConfig: false));
            }
        }

        [Fact]
        public void IsDirty_WithConfig()
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
                Assert.True(gitBase.IsDirty(excludeHarmonizeConfig: true, regenerateConfig: false));
            }
        }

        [Fact]
        public void IsDirty_OnlyConfig_Include()
        {
            using (var repo = Repository_Tools.GetStandardRepo())
            {
                File.WriteAllText(
                    Path.Combine(repo.Repo.Info.WorkingDirectory, HarmonizeGitBase.HarmonizeConfigPath),
                    "dirty work");
                HarmonizeGitBase gitBase = GetHarmonize(repo.Repo.Info.WorkingDirectory);
                Assert.True(gitBase.IsDirty(excludeHarmonizeConfig: false, regenerateConfig: false));
            }
        }

        [Fact]
        public void IsDirty_OnlyConfig_Exclude()
        {
            using (var repo = Repository_Tools.GetStandardRepo())
            {
                File.WriteAllText(
                    Path.Combine(repo.Repo.Info.WorkingDirectory, HarmonizeGitBase.HarmonizeConfigPath),
                    "dirty work");
                HarmonizeGitBase gitBase = GetHarmonize(repo.Repo.Info.WorkingDirectory);
                Assert.False(gitBase.IsDirty(excludeHarmonizeConfig: true, regenerateConfig: false));
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
                Assert.Empty(checkout.Harmonize.GetReposWithUncommittedChanges());
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
                var changes = checkout.Harmonize.GetReposWithUncommittedChanges();
                Assert.Equal(1, changes.Count);
                Assert.Equal(changes[0].Path, parentRepo.Dir.FullName);
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
                var changes = checkout.Harmonize.GetReposWithUncommittedChanges();
                Assert.Equal(0, changes.Count);
            }
        }
        #endregion
    }
}
