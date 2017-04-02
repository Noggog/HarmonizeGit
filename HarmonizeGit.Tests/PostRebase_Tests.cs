using FishingWithGit;
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
    public class PostRebase_Tests
    {
        public string AncestorSha;
        public string RebasedSha;
        public string OldSha;
        public string FillerSha;

        public ConfigCheckout GetCheckout()
        {
            var checkout = Repository_Tools.GetStandardConfigCheckout();
            var parentCommit = checkout.ParentRepo.Repo.Lookup<Commit>(Repository_Tools.STANDARD_CONFIG_PARENT_SECOND_COMMIT);
            this.AncestorSha = checkout.Repo.Repo.Head.Tip.Sha;
            var signature = Repository_Tools.GetSignature();

            File.WriteAllText(checkout.Repo.Repo.Info.WorkingDirectory + Repository_Tools.STANDARD_FILE, "Dirty");
            Commands.Stage(checkout.Repo.Repo, Repository_Tools.STANDARD_FILE);
            var oldCommit = checkout.Repo.Repo.Commit(
                "Commit to rebase",
                signature,
                signature);
            this.OldSha = oldCommit.Sha;

            checkout.Harmonize.ChildLoader.InsertChildEntry(
                new ChildUsage()
                {
                    Sha = this.OldSha,
                    ParentSha = checkout.ParentRepo.Repo.Head.Tip.Sha,
                    ChildRepoPath = checkout.Repo.Dir.FullName,
                    ParentRepoPath = checkout.ParentRepo.Dir.FullName
                });

            signature = new Signature(signature.Name, signature.Email, DateTime.Now);
            checkout.Repo.Repo.Reset(ResetMode.Hard, this.AncestorSha);

            File.WriteAllText(checkout.Repo.Repo.Info.WorkingDirectory + Repository_Tools.STANDARD_FILE, "Prep");
            Commands.Stage(checkout.Repo.Repo, Repository_Tools.STANDARD_FILE);
            var fillerCommit = checkout.Repo.Repo.Commit(
                "I'm just a commit",
                signature,
                signature);
            this.FillerSha = fillerCommit.Sha;

            File.WriteAllText(checkout.Repo.Repo.Info.WorkingDirectory + Repository_Tools.STANDARD_FILE, "Dirty");
            Commands.Stage(checkout.Repo.Repo, Repository_Tools.STANDARD_FILE);
            var rebased = checkout.Repo.Repo.Commit(
                "Commit to rebase",
                signature,
                signature);
            this.RebasedSha = rebased.Sha;
            return checkout;
        }

        [Fact]
        public async Task RemoveUsagesFromParent()
        {
            using (var checkout = GetCheckout())
            {
                var childGet = await checkout.ParentHarmonize.ChildLoader.LookupChildUsage(this.OldSha);
                Assert.True(childGet.Succeeded);
                var handler = new PostRebaseHandler(checkout.Harmonize);
                var args = new RebaseInProgressArgs()
                {
                    OriginalTipSha = this.OldSha,
                    LandingSha = this.AncestorSha
                };
                await handler.Handle(args.ToArray());
                childGet = await checkout.ParentHarmonize.ChildLoader.LookupChildUsage(this.OldSha);
                Assert.False(childGet.Succeeded);
            }
        }

        [Fact]
        public async Task InsertNewCommitsIntoParents()
        {
            using (var checkout = GetCheckout())
            {
                var childGet = await checkout.ParentHarmonize.ChildLoader.LookupChildUsage(this.RebasedSha);
                Assert.False(childGet.Succeeded);
                var handler = new PostRebaseHandler(checkout.Harmonize);
                var args = new RebaseInProgressArgs()
                {
                    OriginalTipSha = this.OldSha,
                    LandingSha = this.AncestorSha
                };
                await handler.Handle(args.ToArray());
                childGet = await checkout.ParentHarmonize.ChildLoader.LookupChildUsage(this.RebasedSha);
                Assert.True(childGet.Succeeded);
                Assert.Equal(this.RebasedSha, childGet.Usage.Sha);
                Assert.Equal(checkout.Repo.Dir.FullName, childGet.Usage.ChildRepoPath);
                Assert.Equal(checkout.ParentRepo.Repo.Head.Tip.Sha, childGet.Usage.ParentSha);
                Assert.Equal(checkout.ParentRepo.Dir.FullName, childGet.Usage.ParentRepoPath);
            }
        }
    }
}
