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
    public class RepoCheckout : IDisposable
    {
        public readonly Repository Repo;
        public readonly DirectoryInfo Dir;

        public RepoCheckout(
            Repository repo,
            DirectoryInfo dir)
        {
            this.Repo = repo;
            this.Dir = dir;
        }

        private void DeleteDirectory(string targetDir)
        {
            File.SetAttributes(targetDir, FileAttributes.Normal);

            string[] files = Directory.GetFiles(targetDir);
            string[] dirs = Directory.GetDirectories(targetDir);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(targetDir, false);
        }

        public void Dispose()
        {
            this.Repo.Dispose();
            DeleteDirectory(this.Dir.FullName);
        }
    }

    public class ConfigCheckout : IDisposable
    {
        public RepoCheckout SuperParentRepo;
        public RepoCheckout ParentRepo;
        public RepoCheckout Repo;
        public HarmonizeGitBase Harmonize;
        public HarmonizeGitBase ParentHarmonize;
        public HarmonizeGitBase SuperParentHarmonize;
        public HarmonizeConfig Config => Harmonize.Config;

        public string SuperParent_FirstSha;
        public string SuperParent_SecondSha;

        public string Parent_FirstSha;
        public string Parent_SecondSha;
        public string Parent_ThirdSha;

        public string Child_FirstSha;
        public string Child_SecondSha;
        public string Child_ThirdSha;
        public string Child_FourthSha;

        public ConfigCheckout(
            Repository superParentRepo,
            Repository parentRepo,
            Repository repo)
        {
            this.Harmonize = new HarmonizeGitBase(repo.Info.WorkingDirectory);
            this.ParentHarmonize = new HarmonizeGitBase(parentRepo.Info.WorkingDirectory);
            this.SuperParentHarmonize = new HarmonizeGitBase(superParentRepo.Info.WorkingDirectory);
            this.ParentRepo = new RepoCheckout(parentRepo, new DirectoryInfo(parentRepo.Info.WorkingDirectory));
            this.Repo = new RepoCheckout(repo, new DirectoryInfo(repo.Info.WorkingDirectory));
            this.SuperParentRepo = new RepoCheckout(superParentRepo, new DirectoryInfo(superParentRepo.Info.WorkingDirectory));
        }

        public void Init()
        {
            this.Harmonize.Init();
            this.ParentHarmonize.Init();
            this.SuperParentHarmonize.Init();
        }

        public void Dispose()
        {
            this.Repo.Dispose();
            this.ParentRepo.Dispose();
            this.SuperParentRepo.Dispose();
        }
    }

    class Repository_Tools
    {
        public static DirectoryInfo GetTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()) + "\\";
            Directory.CreateDirectory(tempDirectory);
            return new DirectoryInfo(tempDirectory);
        }

        public static Signature GetSignature()
        {
            var date = new DateTime(2016, 03, 10);
            var signature = new Signature(
                "Justin Swanson",
                "justin.c.swanson@gmail.com",
                date);
            return signature;
        }

        public const string STANDARD_MERGE_BRANCH = "Merge";
        public const string STANDARD_DETATCHED_BRANCH = "Detached";
        public const string STANDARD_FILE = "Test.txt";

        public static RepoCheckout GetStandardRepo()
        {
            var signature = GetSignature();
            var dir = GetTemporaryDirectory();
            Repository.Init(dir.FullName);
            var repo = new Repository(dir.FullName);
            var aFile = new FileInfo(Path.Combine(dir.FullName, STANDARD_FILE));
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
            return new RepoCheckout(
                repo,
                dir);
        }

        public static ConfigCheckout GetStandardConfigCheckout()
        {
            var signature = GetSignature();

            var superParentRepoDir = GetTemporaryDirectory();
            Repository.Init(superParentRepoDir.FullName);
            var superParentRepo = new Repository(superParentRepoDir.FullName);
            var superParentFile = new FileInfo(Path.Combine(superParentRepoDir.FullName, STANDARD_FILE));
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

            var superParentListing = new RepoListing()
            {
                Nickname = "SuperParentRepo",
                SuggestedPath = superParentRepoDir.FullName,
                Path = superParentRepoDir.FullName,
            };
            superParentListing.SetToCommit(secondSuperParentCommit);
            HarmonizeConfig parentConfig = new HarmonizeConfig();
            parentConfig.ParentRepos.Add(superParentListing);
            var parentRepoDir = GetTemporaryDirectory();
            Repository.Init(parentRepoDir.FullName);
            var parentRepo = new Repository(parentRepoDir.FullName);
            var parentFile = new FileInfo(Path.Combine(parentRepoDir.FullName, STANDARD_FILE));
            var parentHarmonizeFile = new FileInfo(Path.Combine(parentRepoDir.FullName, HarmonizeGitBase.HarmonizeConfigPath));
            File.WriteAllText(parentHarmonizeFile.FullName, parentConfig.GetXmlStr());
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
            
            var childRepoDir = GetTemporaryDirectory();
            Repository.Init(childRepoDir.FullName);
            var childRepo = new Repository(childRepoDir.FullName);
            var childHarmonizeFile = new FileInfo(Path.Combine(childRepoDir.FullName, HarmonizeGitBase.HarmonizeConfigPath));
            var parentListing = new RepoListing()
            {
                Nickname = "ParentRepo",
                SuggestedPath = parentRepoDir.FullName,
                Path = parentRepoDir.FullName,
            };
            HarmonizeConfig childConfig = new HarmonizeConfig();
            childConfig.ParentRepos.Add(superParentListing);
            childConfig.ParentRepos.Add(parentListing);

            var childFile = new FileInfo(Path.Combine(childRepoDir.FullName, STANDARD_FILE));
            File.WriteAllText(childFile.FullName, "Child123\n");
            Commands.Stage(childRepo, childFile.FullName);
            parentListing.SetToCommit(firstParentCommit);
            File.WriteAllText(childHarmonizeFile.FullName, childConfig.GetXmlStr());
            Commands.Stage(childRepo, childHarmonizeFile.FullName);
            var commit1 = childRepo.Commit(
                "A Commit",
                signature,
                signature);
            File.WriteAllText(childFile.FullName, "Child456\n");
            Commands.Stage(childRepo, childFile.FullName);
            parentListing.SetToCommit(secondCommit);
            File.WriteAllText(childHarmonizeFile.FullName, childConfig.GetXmlStr());
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
            File.WriteAllText(childHarmonizeFile.FullName, childConfig.GetXmlStr());
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
            };
        }
    }
}
