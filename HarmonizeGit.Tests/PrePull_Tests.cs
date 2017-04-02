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
    public class PrePull_Tests
    {
        [Fact]
        public async Task CleanSetup()
        {
            using (var checkout = Repository_Tools.GetStandardConfigCheckout())
            {
                checkout.Init();
                var parentCommit = checkout.ParentRepo.Repo.Lookup<Commit>(checkout.Parent_SecondSha);
                Assert.False(checkout.Repo.Repo.RetrieveStatus().IsDirty);
                var pull = new PrePullHandler(checkout.Harmonize);
                await pull.Handle(null);
                Assert.False(checkout.Repo.Repo.RetrieveStatus().IsDirty);
            }
        }

        [Fact]
        public async Task DirtyConfig()
        {
            using (var checkout = Repository_Tools.GetStandardConfigCheckout())
            {
                checkout.Init();
                var parentCommit = checkout.ParentRepo.Repo.Lookup<Commit>(checkout.Parent_SecondSha);
                checkout.Harmonize.Config.ParentRepos[0].SetToCommit(parentCommit);
                File.WriteAllText(checkout.Repo.Repo.Info.WorkingDirectory + HarmonizeGitBase.HarmonizeConfigPath, checkout.Harmonize.Config.GetXmlStr());
                Assert.True(checkout.Repo.Repo.RetrieveStatus().IsDirty);
                var pull = new PrePullHandler(checkout.Harmonize);
                await pull.Handle(null);
                Assert.False(checkout.Repo.Repo.RetrieveStatus().IsDirty);
            }
        }

        [Fact]
        public async Task KeepsOtherChanges()
        {
            using (var checkout = Repository_Tools.GetStandardConfigCheckout())
            {
                checkout.Init();
                var parentCommit = checkout.ParentRepo.Repo.Lookup<Commit>(checkout.Parent_SecondSha);
                checkout.Harmonize.Config.ParentRepos[0].SetToCommit(parentCommit);
                File.WriteAllText(checkout.Repo.Repo.Info.WorkingDirectory + HarmonizeGitBase.HarmonizeConfigPath, checkout.Harmonize.Config.GetXmlStr());
                File.WriteAllText(checkout.Repo.Repo.Info.WorkingDirectory + Repository_Tools.STANDARD_FILE, "Dirty");
                Assert.True(checkout.Repo.Repo.RetrieveStatus().IsDirty);
                var pull = new PrePullHandler(checkout.Harmonize);
                await pull.Handle(null);
                Assert.True(checkout.Repo.Repo.RetrieveStatus().IsDirty);
            }
        }
    }
}
