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
    public class PostPull_Tests
    {
        [Fact]
        public async Task InsertNewStrandedCommits()
        {
            using (var checkout = Repository_Tools.GetStandardConfigCheckout())
            {
                await checkout.Init();
                var parentCommit = checkout.ParentRepo.Repo.Lookup<Commit>(checkout.Parent_SecondSha);
                var ancestorSha = checkout.Repo.Repo.Head.Tip.Sha;

                File.WriteAllText(checkout.Repo.Repo.Info.WorkingDirectory + Utility.STANDARD_FILE, "Dirty");
                Commands.Stage(checkout.Repo.Repo, Utility.STANDARD_FILE);
                var commit = checkout.Repo.Repo.Commit(
                    "A Commit",
                    Utility.GetSignature(),
                    Utility.GetSignature());
                File.WriteAllText(checkout.Repo.Repo.Info.WorkingDirectory + Utility.STANDARD_FILE, "StillDirty");
                Commands.Stage(checkout.Repo.Repo, Utility.STANDARD_FILE);
                var commit2 = checkout.Repo.Repo.Commit(
                    "A Commit",
                    Utility.GetSignature(),
                    Utility.GetSignature());
                var args = new PullArgs()
                {
                    AncestorSha = ancestorSha,
                    TargetSha = checkout.Repo.Repo.Head.Tip.Sha
                };
                var handler = new PostPullHandler(checkout.Harmonize, args);
                await handler.Handle();
                var childGet = await checkout.ParentHarmonize.ChildLoader.LookupChildUsage(commit.Sha);
                Assert.True(childGet.Succeeded);
                Assert.Equal(commit.Sha, childGet.Usage.Sha);
                Assert.Equal(checkout.Repo.Dir.FullName, childGet.Usage.ChildRepoPath);
                Assert.Equal(checkout.ParentRepo.Repo.Head.Tip.Sha, childGet.Usage.ParentSha);
                Assert.Equal(checkout.ParentRepo.Dir.FullName, childGet.Usage.ParentRepoPath);
                childGet = await checkout.ParentHarmonize.ChildLoader.LookupChildUsage(commit2.Sha);
                Assert.True(childGet.Succeeded);
                Assert.Equal(commit2.Sha, childGet.Usage.Sha);
                Assert.Equal(checkout.Repo.Dir.FullName, childGet.Usage.ChildRepoPath);
                Assert.Equal(checkout.ParentRepo.Repo.Head.Tip.Sha, childGet.Usage.ParentSha);
                Assert.Equal(checkout.ParentRepo.Dir.FullName, childGet.Usage.ParentRepoPath);
            }
        }
    }
}
