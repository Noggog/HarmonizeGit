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
    public class PostReset_Tests
    {
        [Fact]
        public async Task ParentCheckout()
        {
            using (var checkout = Repository_Tools.GetStandardConfigCheckout())
            {
                Commands.Checkout(checkout.Repo.Repo, checkout.Child_ThirdSha, new CheckoutOptions() { CheckoutModifiers = CheckoutModifiers.Force });
                await checkout.Init();
                ResetArgs args = new ResetArgs()
                {
                    StartingSha = checkout.Child_FourthSha,
                    TargetSha = checkout.Child_ThirdSha,
                    Type = ResetType.hard
                };
                PostResetHandler handler = new PostResetHandler(checkout.Harmonize);
                await handler.Handle(args.ToArray());
                var parentSha = checkout.ParentRepo.Repo.Head.Tip.Sha;
                Assert.Equal(
                    checkout.Parent_SecondSha,
                    parentSha);
            }
        }

        [Fact]
        public async Task InsertNewCommits()
        {
            using (var checkout = Repository_Tools.GetStandardConfigCheckout())
            {
                await checkout.Init();
                File.WriteAllText(checkout.Repo.Repo.Info.WorkingDirectory + Utility.STANDARD_FILE, "Prep");
                Commands.Stage(checkout.Repo.Repo, Utility.STANDARD_FILE);
                var commit = checkout.Repo.Repo.Commit(
                    "I'm just a commit",
                    Utility.GetSignature(),
                    Utility.GetSignature());
                ResetArgs args = new ResetArgs()
                {
                    StartingSha = checkout.Child_FourthSha,
                    TargetSha = checkout.Child_ThirdSha,
                    Type = ResetType.hard
                };
                PostResetHandler handler = new PostResetHandler(checkout.Harmonize);
                await handler.Handle(args.ToArray());
                var get = await checkout.ParentHarmonize.ChildLoader.LookupChildUsage(commit.Sha);
                Assert.True(get.Succeeded);
            }
        }
    }
}
