﻿using FishingWithGit;
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
    public class PrePull_Tests
    {
        [Fact]
        public async Task CleanSetup()
        {
            using (var checkout = Repository_Tools.GetStandardConfigCheckout())
            {
                await checkout.Init();
                var parentCommit = checkout.ParentRepo.Repo.Lookup<Commit>(checkout.Parent_SecondSha);
                Assert.False(checkout.Repo.Repo.RetrieveStatus().IsDirty);
                var pull = new PrePullHandler(checkout.Harmonize, new PullArgs());
                await pull.Handle();
                Assert.False(checkout.Repo.Repo.RetrieveStatus().IsDirty);
            }
        }

        [Fact]
        public async Task DirtyConfig()
        {
            using (var checkout = Repository_Tools.GetStandardConfigCheckout())
            {
                await checkout.Init();
                var parentCommit = checkout.ParentRepo.Repo.Lookup<Commit>(checkout.Parent_SecondSha);
                checkout.Harmonize.Config.ParentRepos[0].SetToCommit(parentCommit);
                checkout.Harmonize.Config.WriteToPath(checkout.Repo.Repo.Info.WorkingDirectory + Constants.HarmonizeConfigPath);
                Assert.True(checkout.Repo.Repo.RetrieveStatus().IsDirty);
                var pull = new PrePullHandler(checkout.Harmonize, new PullArgs());
                await pull.Handle();
                Assert.False(checkout.Repo.Repo.RetrieveStatus().IsDirty);
            }
        }

        [Fact]
        public async Task KeepsOtherChanges()
        {
            using (var checkout = Repository_Tools.GetStandardConfigCheckout())
            {
                await checkout.Init();
                var parentCommit = checkout.ParentRepo.Repo.Lookup<Commit>(checkout.Parent_SecondSha);
                checkout.Harmonize.Config.ParentRepos[0].SetToCommit(parentCommit);
                checkout.Harmonize.Config.WriteToPath(checkout.Repo.Repo.Info.WorkingDirectory + Constants.HarmonizeConfigPath);
                File.WriteAllText(checkout.Repo.Repo.Info.WorkingDirectory + Utility.STANDARD_FILE, "Dirty");
                Assert.True(checkout.Repo.Repo.RetrieveStatus().IsDirty);
                var pull = new PrePullHandler(checkout.Harmonize, new PullArgs());
                await pull.Handle();
                Assert.True(checkout.Repo.Repo.RetrieveStatus().IsDirty);
            }
        }
    }
}
