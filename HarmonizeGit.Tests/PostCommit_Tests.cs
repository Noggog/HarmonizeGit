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
    public class PostCommit_Tests
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
                var handler = new PostCommitHandler(checkout.Harmonize, new CommitArgs());
                await handler.Handle();
                var childGet = await checkout.ParentHarmonize.ChildLoader.LookupChildUsage(commit.Sha);
                Assert.True(childGet.Succeeded);
                Assert.Equal(commit.Sha, childGet.Item.Sha);
                Assert.Equal(checkout.Repo.Dir.FullName, childGet.Item.ChildRepoPath);
                Assert.Equal(checkout.ParentRepo.Repo.Head.Tip.Sha, childGet.Item.ParentSha);
                Assert.Equal(checkout.ParentRepo.Dir.FullName, childGet.Item.ParentRepoPath);
            }
        }
    }
}
