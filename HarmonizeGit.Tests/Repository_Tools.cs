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
    public class ConfigCheckout : IDisposable
    {
        public RepoCheckout SuperParentRepo;
        public RepoCheckout ParentRepo;
        public RepoCheckout Repo;
        public HarmonizeGitBase Harmonize;
        public HarmonizeGitBase ParentHarmonize;
        public HarmonizeGitBase SuperParentHarmonize;

        public RepoListing ChildToParentListing;
        public RepoListing ChildToSuperParentListing;
        public RepoListing ParentToSuperParentListing;

        public string SuperParent_FirstSha;
        public string SuperParent_SecondSha;

        public string Parent_FirstSha;
        public string Parent_SecondSha;
        public string Parent_ThirdSha;

        public string Child_FirstSha;
        public string Child_SecondSha;
        public string Child_ThirdSha;
        public string Child_FourthSha;

        public FileInfo ChildFile;
        public FileInfo ParentFile;
        public FileInfo SuperParentFile;

        public ConfigCheckout(
            Repository superParentRepo,
            Repository parentRepo,
            Repository repo)
        {
            this.Harmonize = new HarmonizeGitBase(repo.Info.WorkingDirectory);
            this.ParentHarmonize = new HarmonizeGitBase(parentRepo.Info.WorkingDirectory);
            this.SuperParentHarmonize = new HarmonizeGitBase(superParentRepo.Info.WorkingDirectory);
            this.ParentRepo = new RepoCheckout(parentRepo);
            this.Repo = new RepoCheckout(repo);
            this.SuperParentRepo = new RepoCheckout(superParentRepo);
        }

        public async Task Init()
        {
            this.Harmonize.Init();
            this.ParentHarmonize.Init();
            this.SuperParentHarmonize.Init();
            this.ChildToParentListing = this.Harmonize.Config.ParentRepos.Where((l) => l.Path.Equals(ParentRepo.Dir.FullName)).First();
            this.ChildToSuperParentListing = this.Harmonize.Config.ParentRepos.Where((l) => l.Path.Equals(SuperParentRepo.Dir.FullName)).First();
            this.ParentToSuperParentListing = this.ParentHarmonize.Config.ParentRepos.Where((l) => l.Path.Equals(SuperParentRepo.Dir.FullName)).First();
            await Task.WhenAll(
                this.Harmonize.ChildLoader.InitializeIntoParents(),
                this.ParentHarmonize.ChildLoader.InitializeIntoParents());
        }

        public void Dispose()
        {
            this.Repo.Dispose();
            this.ParentRepo.Dispose();
            this.SuperParentRepo.Dispose();
        }
    }

    public class CloneCheckout : IDisposable
    {
        public ConfigCheckout Local;

        public Repository ChildRemoteRepo;
        public Repository ParentRemoteRepo;
        public Repository SuperParentRemoteRepo;

        public Remote ChildRemote;
        public Remote ParentRemote;
        public Remote SuperParentRemote;

        public void Dispose()
        {
            Local.Dispose();
            ChildRemote.Dispose();
            ParentRemote.Dispose();
            SuperParentRemote.Dispose();
        }

        public async Task Init()
        {
            await Local.Init();
        }
    }

    class Repository_Tools
    {
        public const string STANDARD_MERGE_BRANCH = "Merge";
        public const string STANDARD_DETATCHED_BRANCH = "Detached";

        public static RepoCheckout GetStandardRepo()
        {
            var signature = Utility.GetSignature();
            var dir = Utility.GetTemporaryDirectory();
            Repository.Init(dir.FullName);
            var repo = new Repository(dir.FullName);
            var aFile = new FileInfo(Path.Combine(dir.FullName, Utility.STANDARD_FILE));
            File.WriteAllText(aFile.FullName, "Testing123\n");
            Commands.Stage(repo, aFile.FullName);
            var firstCommit = repo.Commit(
                "First Commit",
                signature,
                signature);
            File.WriteAllText(aFile.FullName, "Testing456\n");
            Commands.Stage(repo, aFile.FullName);
            var secondCommit = repo.Commit(
                "Second Commit",
                signature,
                signature);
            Commands.Checkout(repo, firstCommit);
            File.WriteAllText(aFile.FullName, "Testing789\n");
            Commands.Stage(repo, aFile.FullName);
            var splitCommit = repo.Commit(
                "Split Commit",
                signature,
                signature);
            Commands.Checkout(repo, splitCommit);
            var result = repo.Merge(
                secondCommit,
                signature,
                new MergeOptions()
                {
                    CommitOnSuccess = true,
                    MergeFileFavor = MergeFileFavor.Union,
                });
            repo.CreateBranch(STANDARD_MERGE_BRANCH);
            Assert.False(result.Status == MergeStatus.Conflicts);
            Commands.Checkout(repo, secondCommit);
            File.WriteAllText(aFile.FullName, "TestingAAA\n");
            Commands.Stage(repo, aFile.FullName);
            var detachedCommit = repo.Commit(
                "Detached Commit",
                signature,
                signature);
            repo.CreateBranch(STANDARD_DETATCHED_BRANCH);
            return new RepoCheckout(repo);
        }

        public static ConfigCheckout GetStandardConfigCheckout()
        {
            var signature = Utility.GetSignature();

            var superParentRepoDir = Utility.GetTemporaryDirectory();
            Repository.Init(superParentRepoDir.FullName);
            var superParentRepo = new Repository(superParentRepoDir.FullName);
            var superParentFile = new FileInfo(Path.Combine(superParentRepoDir.FullName, Utility.STANDARD_FILE));
            File.WriteAllText(superParentFile.FullName, "Testing123\n");
            Commands.Stage(superParentRepo, superParentFile.FullName);
            var firstSuperParentCommit = superParentRepo.Commit(
                "First Commit",
                signature,
                signature);
            File.WriteAllText(superParentFile.FullName, "Testing456\n");
            Commands.Stage(superParentRepo, superParentFile.FullName);
            var secondSuperParentCommit = superParentRepo.Commit(
                "Second Commit",
                signature,
                signature);

            var parentToSuperParentListing = new RepoListing()
            {
                Nickname = "SuperParentRepo",
                SuggestedPath = superParentRepoDir.FullName,
                Path = superParentRepoDir.FullName,
            };
            parentToSuperParentListing.SetToCommit(secondSuperParentCommit);
            HarmonizeConfig parentConfig = new HarmonizeConfig();
            parentConfig.ParentRepos.Add(parentToSuperParentListing);
            var parentRepoDir = Utility.GetTemporaryDirectory();
            Repository.Init(parentRepoDir.FullName);
            var parentRepo = new Repository(parentRepoDir.FullName);
            var parentFile = new FileInfo(Path.Combine(parentRepoDir.FullName, Utility.STANDARD_FILE));
            var parentHarmonizeFile = new FileInfo(Path.Combine(parentRepoDir.FullName, HarmonizeGitBase.HarmonizeConfigPath));
            parentConfig.WriteToPath(parentHarmonizeFile.FullName);
            Commands.Stage(parentRepo, parentHarmonizeFile.FullName);
            File.WriteAllText(parentFile.FullName, "Testing123\n");
            Commands.Stage(parentRepo, parentFile.FullName);
            var firstParentCommit = parentRepo.Commit(
                "First Commit",
                signature,
                signature);
            File.WriteAllText(parentFile.FullName, "Testing456\n");
            Commands.Stage(parentRepo, parentFile.FullName);
            var secondCommit = parentRepo.Commit(
                "Second Commit",
                signature,
                signature);
            File.WriteAllText(parentFile.FullName, "Testing789\n");
            Commands.Stage(parentRepo, parentFile.FullName);
            var thirdCommit = parentRepo.Commit(
                "Third Commit",
                signature,
                signature);

            var childRepoDir = Utility.GetTemporaryDirectory();
            Repository.Init(childRepoDir.FullName);
            var childRepo = new Repository(childRepoDir.FullName);
            var childHarmonizeFile = new FileInfo(Path.Combine(childRepoDir.FullName, HarmonizeGitBase.HarmonizeConfigPath));
            var parentListing = new RepoListing()
            {
                Nickname = "ParentRepo",
                SuggestedPath = parentRepoDir.FullName,
                Path = parentRepoDir.FullName,
            };
            var superParentListing = new RepoListing()
            {
                Nickname = "SuperParentRepo",
                SuggestedPath = superParentRepoDir.FullName,
                Path = superParentRepoDir.FullName,
            };
            superParentListing.SetToCommit(secondSuperParentCommit);
            HarmonizeConfig childConfig = new HarmonizeConfig();
            childConfig.ParentRepos.Add(superParentListing);
            childConfig.ParentRepos.Add(parentListing);

            var childFile = new FileInfo(Path.Combine(childRepoDir.FullName, Utility.STANDARD_FILE));
            File.WriteAllText(childFile.FullName, "Child123\n");
            Commands.Stage(childRepo, childFile.FullName);
            parentListing.SetToCommit(firstParentCommit);
            childConfig.WriteToPath(childHarmonizeFile.FullName);
            Commands.Stage(childRepo, childHarmonizeFile.FullName);
            var commit1 = childRepo.Commit(
                "A Commit",
                signature,
                signature);
            File.WriteAllText(childFile.FullName, "Child456\n");
            Commands.Stage(childRepo, childFile.FullName);
            parentListing.SetToCommit(secondCommit);
            childConfig.WriteToPath(childHarmonizeFile.FullName);
            Commands.Stage(childRepo, childHarmonizeFile.FullName);
            var commit2 = childRepo.Commit(
                "A Commit",
                signature,
                signature);
            File.WriteAllText(childFile.FullName, "Child789\n");
            Commands.Stage(childRepo, childFile.FullName);
            var commit3 = childRepo.Commit(
                "A Commit",
                signature,
                signature);
            File.WriteAllText(childFile.FullName, "Child101112\n");
            Commands.Stage(childRepo, childFile.FullName);
            parentListing.SetToCommit(thirdCommit);
            childConfig.WriteToPath(childHarmonizeFile.FullName);
            Commands.Stage(childRepo, childHarmonizeFile.FullName);
            var commit4 = childRepo.Commit(
                "A Commit",
                signature,
                signature);

            return new ConfigCheckout(superParentRepo, parentRepo, childRepo)
            {
                SuperParent_FirstSha = firstSuperParentCommit.Sha,
                SuperParent_SecondSha = secondSuperParentCommit.Sha,
                Parent_FirstSha = firstParentCommit.Sha,
                Parent_SecondSha = secondCommit.Sha,
                Parent_ThirdSha = thirdCommit.Sha,
                Child_FirstSha = commit1.Sha,
                Child_SecondSha = commit2.Sha,
                Child_ThirdSha = commit3.Sha,
                Child_FourthSha = commit4.Sha,
                ChildFile = childFile,
                ParentFile = parentFile,
                SuperParentFile = superParentFile,
            };
        }

        public static CloneCheckout GetStandardCloneCheckout()
        {
            var clone = new CloneCheckout()
            {
                Local = GetStandardConfigCheckout()
            };

            var superParentRepoDir = Utility.GetTemporaryDirectory();
            var parentRepoDir = Utility.GetTemporaryDirectory();
            var repoDir = Utility.GetTemporaryDirectory();

            Repository.Init(superParentRepoDir.FullName, isBare: true);
            Repository.Init(parentRepoDir.FullName, isBare: true);
            Repository.Init(repoDir.FullName, isBare: true);

            clone.SuperParentRemoteRepo = new Repository(superParentRepoDir.FullName);
            clone.ParentRemoteRepo = new Repository(parentRepoDir.FullName);
            clone.ChildRemoteRepo = new Repository(repoDir.FullName);

            clone.SuperParentRemote = clone.Local.SuperParentRepo.Repo.Network.Remotes.Add("origin", superParentRepoDir.FullName);
            clone.ParentRemote = clone.Local.ParentRepo.Repo.Network.Remotes.Add("origin", parentRepoDir.FullName);
            clone.ChildRemote = clone.Local.Repo.Repo.Network.Remotes.Add("origin", repoDir.FullName);

            clone.Local.SuperParentRepo.Repo.Network.Push(
                clone.SuperParentRemote,
                pushRefSpec: @"refs/heads/master");
            clone.Local.ParentRepo.Repo.Network.Push(
                clone.ParentRemote,
                pushRefSpec: @"refs/heads/master");
            clone.Local.Repo.Repo.Network.Push(
                clone.ChildRemote,
                pushRefSpec: @"refs/heads/master");

            return clone;
        }
    }
}
