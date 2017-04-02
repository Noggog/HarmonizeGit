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
                checkout.Init();
                var parentCommit = checkout.ParentRepo.Repo.Lookup<Commit>(checkout.Parent_SecondSha);
                File.WriteAllText(checkout.Repo.Repo.Info.WorkingDirectory + Repository_Tools.STANDARD_FILE, "Dirty");
                Commands.Stage(checkout.Repo.Repo, Repository_Tools.STANDARD_FILE);
                var commit = checkout.Repo.Repo.Commit(
                    "A Commit",
                    Repository_Tools.GetSignature(),
                    Repository_Tools.GetSignature());
                var handler = new PostCommitHandler(checkout.Harmonize);
                await handler.Handle(null);
                var childGet = await checkout.ParentHarmonize.ChildLoader.LookupChildUsage(commit.Sha);
                Assert.True(childGet.Succeeded);
                Assert.Equal(commit.Sha, childGet.Usage.Sha);
                Assert.Equal(checkout.Repo.Dir.FullName, childGet.Usage.ChildRepoPath);
                Assert.Equal(checkout.ParentRepo.Repo.Head.Tip.Sha, childGet.Usage.ParentSha);
                Assert.Equal(checkout.ParentRepo.Dir.FullName, childGet.Usage.ParentRepoPath);
            }
        }
    }
}
