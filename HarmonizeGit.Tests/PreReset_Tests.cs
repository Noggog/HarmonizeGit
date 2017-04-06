using FishingWithGit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace HarmonizeGit.Tests
{
    public class PreReset_Tests
    {
        [Fact]
        public async Task BlockIfChildrenAreUsing()
        {
            using (var checkout = Repository_Tools.GetStandardConfigCheckout())
            {
                await checkout.Init();

                ResetArgs args = new ResetArgs()
                {
                    StartingSha = checkout.ParentRepo.Repo.Head.Tip.Sha,
                    TargetSha = checkout.Parent_SecondSha,
                    Type = ResetType.hard
                };
                PreResetHandler handler = new PreResetHandler(checkout.ParentHarmonize, args);
                var ret = await handler.Handle();
                Assert.False(ret);
            }
        }

        [Fact]
        public async Task RemoveUsageFromParentDatabase()
        {
            using (var checkout = Repository_Tools.GetStandardConfigCheckout())
            {
                await checkout.Init();

                ResetArgs args = new ResetArgs()
                {
                    StartingSha = checkout.Repo.Repo.Head.Tip.Sha,
                    TargetSha = checkout.Child_ThirdSha,
                    Type = ResetType.hard
                };
                PreResetHandler handler = new PreResetHandler(checkout.Harmonize, args);
                var ret = await handler.Handle();
                Assert.True(ret);
                var usage = await checkout.Harmonize.ChildLoader.LookupChildUsage(checkout.Child_FourthSha);
                Assert.False(usage.Succeeded);
            }
        }
    }
}
