using HarmonizeGitHooks;
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
    public class HarmonizeGit_Tests
    {
        [Fact]
        public void IsDirty_Normal()
        {
            using (var repo = Repository_Tools.GetStandardRepo())
            {
                File.WriteAllText(
                    Path.Combine(repo.Repo.Info.WorkingDirectory, Repository_Tools.STANDARD_FILE),
                    "dirty work");
                HarmonizeGitBase gitBase = new HarmonizeGitBase(repo.Repo.Info.WorkingDirectory);
                Assert.True(gitBase.IsDirty(excludeHarmonizeConfig: false, regenerateConfig: false));
            }
        }

        [Fact]
        public void IsDirty_NotDirty()
        {
            using (var repo = Repository_Tools.GetStandardRepo())
            {
                HarmonizeGitBase gitBase = new HarmonizeGitBase(repo.Repo.Info.WorkingDirectory);
                Assert.False(gitBase.IsDirty(excludeHarmonizeConfig: false, regenerateConfig: false));
            }
        }

        [Fact]
        public void IsDirty_WithConfig()
        {
            using (var repo = Repository_Tools.GetStandardRepo())
            {
                File.WriteAllText(
                    Path.Combine(repo.Repo.Info.WorkingDirectory, Repository_Tools.STANDARD_FILE),
                    "dirty work");
                File.WriteAllText(
                    Path.Combine(repo.Repo.Info.WorkingDirectory, HarmonizeGitBase.HarmonizeConfigPath),
                    "dirty work");
                HarmonizeGitBase gitBase = new HarmonizeGitBase(repo.Repo.Info.WorkingDirectory);
                Assert.True(gitBase.IsDirty(excludeHarmonizeConfig: true, regenerateConfig: false));
            }
        }

       [Fact]
        public void IsDirty_OnlyConfig_Include()
        {
            using (var repo = Repository_Tools.GetStandardRepo())
            {
                File.WriteAllText(
                    Path.Combine(repo.Repo.Info.WorkingDirectory, HarmonizeGitBase.HarmonizeConfigPath),
                    "dirty work");
                HarmonizeGitBase gitBase = new HarmonizeGitBase(repo.Repo.Info.WorkingDirectory);
                Assert.True(gitBase.IsDirty(excludeHarmonizeConfig: false, regenerateConfig: false));
            }
        }

        [Fact]
        public void IsDirty_OnlyConfig_Exclude()
        {
            using (var repo = Repository_Tools.GetStandardRepo())
            {
                File.WriteAllText(
                    Path.Combine(repo.Repo.Info.WorkingDirectory, HarmonizeGitBase.HarmonizeConfigPath),
                    "dirty work");
                HarmonizeGitBase gitBase = new HarmonizeGitBase(repo.Repo.Info.WorkingDirectory);
                Assert.False(gitBase.IsDirty(excludeHarmonizeConfig: true, regenerateConfig: false));
            }
        }
    }
}
