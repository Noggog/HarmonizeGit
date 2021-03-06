﻿using FishingWithGit;
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
    public class Take_Tests
    {
        [Fact]
        public async Task SyncParentRepos()
        {
            using (var checkout = Repository_Tools.GetStandardConfigCheckout())
            {
                await checkout.Init();
                var parent2ndCommit = checkout.ParentRepo.Repo.Lookup<Commit>(checkout.Parent_SecondSha);
                checkout.ChildToParentListing.SetToCommit(parent2ndCommit);
                checkout.Harmonize.Config.WriteToPath(Path.Combine(checkout.Repo.Dir.FullName, Constants.HarmonizeConfigPath));
                
                PostTakeHandler handler = new PostTakeHandler(checkout.Harmonize, new TakeArgs());
                var ret = await handler.Handle();
                Assert.True(ret);
                Assert.Equal(checkout.Parent_SecondSha, checkout.ParentRepo.Repo.Head.Tip.Sha);
            }
        }
    }
}
