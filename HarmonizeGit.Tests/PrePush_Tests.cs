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
    public class PrePush_Tests
    {
        [Fact]
        public async Task Typical_Success()
        {
            using (var clone = Repository_Tools.GetStandardCloneCheckout())
            {
                await clone.Init();

                var signature = Utility.GetSignature();
                File.WriteAllText(clone.Local.ChildFile.FullName, "Dirty\n");
                Commands.Stage(clone.Local.Repo.Repo, clone.Local.ChildFile.FullName);
                var commit = clone.Local.Repo.Repo.Commit(
                    "New Commit",
                    signature,
                    signature);

                var args = new PushArgs()
                {
                    Remote = "origin"
                };
                args.RefSpecs.Add(new Tuple<string, string>("master", "master"));
                var push = new PrePushHandler(clone.Local.Harmonize, args);
                var ret = await push.Handle();
                Assert.True(ret);
            }
        }

        [Fact]
        public async Task Typical_Block()
        {
            using (var clone = Repository_Tools.GetStandardCloneCheckout())
            {
                var signature = Utility.GetSignature();

                File.WriteAllText(clone.Local.ParentFile.FullName, "Dirty\n");
                Commands.Stage(clone.Local.ParentRepo.Repo, clone.Local.ParentFile.FullName);
                var parentCommit = clone.Local.ParentRepo.Repo.Commit(
                    "Dirty Commit",
                    signature,
                    signature);

                await clone.Init();
                await clone.Local.Harmonize.SyncConfigToParentShas();

                File.WriteAllText(clone.Local.ChildFile.FullName, "Dirty\n");
                Commands.Stage(clone.Local.Repo.Repo, clone.Local.ChildFile.FullName);
                Commands.Stage(clone.Local.Repo.Repo, clone.Local.Harmonize.ConfigPath);
                var commit = clone.Local.Repo.Repo.Commit(
                    "Dirty Commit",
                    signature,
                    signature);

                var args = new PushArgs()
                {
                    Remote = "origin"
                };
                args.RefSpecs.Add(new Tuple<string, string>("master", "master"));
                var push = new PrePushHandler(clone.Local.Harmonize, args);
                var ret = await push.Handle();
                Assert.False(ret);
            }
        }
    }
}
