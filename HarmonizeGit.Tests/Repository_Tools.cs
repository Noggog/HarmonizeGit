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
        public List<RepoCheckout> ParentRepos = new List<RepoCheckout>();
        public RepoCheckout ParentRepo;
        public RepoCheckout Repo;
        public HarmonizeGitBase Harmonize;
        public HarmonizeGitBase ParentHarmonize;
        public HarmonizeConfig Config => Harmonize.Config;
        public RepoListing ParentListing;

        public ConfigCheckout(
            Repository parentRepo,
            Repository repo)
        {
            this.Harmonize = new HarmonizeGitBase(repo.Info.WorkingDirectory);
            this.Harmonize.Init();
            this.ParentHarmonize = new HarmonizeGitBase(parentRepo.Info.WorkingDirectory);
            this.ParentHarmonize.Init();
            this.ParentRepo = new RepoCheckout(parentRepo, new DirectoryInfo(parentRepo.Info.WorkingDirectory));
            this.Repo = new RepoCheckout(repo, new DirectoryInfo(repo.Info.WorkingDirectory));
            foreach (var parent in this.Harmonize.Config.ParentRepos)
            {
                ParentRepos.Add(
                    new RepoCheckout(
                        new Repository(parent.Path),
                        new DirectoryInfo(parent.Path)));
            }
            this.ParentListing = this.Config.ParentRepos[0];
        }

        public void Dispose()
        {
            this.Repo.Dispose();
            foreach (var repo in ParentRepos)
            {
                repo.Dispose();
            }
        }
    }

    class Repository_Tools
    {
        public static DirectoryInfo GetTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
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

        public const string STANDARD_CONFIG_PARENT_FIRST_COMMIT = "00c373cc3f28fa4bb071e83f5caa7e50676d9431";
        public const string STANDARD_CONFIG_PARENT_SECOND_COMMIT = "2fe948514dee61fd1a164cc7ac5abdc99b6f9bff";
        public const string STANDARD_CONFIG_PARENT_THIRD_COMMIT = "89283a3ff57915fe1856b8bf106eac1aca36a910";

        public static ConfigCheckout GetStandardConfigCheckout()
        {
            var signature = GetSignature();
            var parentRepoDir = GetTemporaryDirectory();
            Repository.Init(parentRepoDir.FullName);
            var parentRepo = new Repository(parentRepoDir.FullName);
            var parentFile = new FileInfo(Path.Combine(parentRepoDir.FullName, STANDARD_FILE));
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
            var harmonizeFile = new FileInfo(Path.Combine(childRepoDir.FullName, HarmonizeGitBase.HarmonizeConfigPath));
            var parentListing = new RepoListing()
            {
                Nickname = "ParentRepo",
                SuggestedPath = parentRepoDir.FullName,
                Path = parentRepoDir.FullName,
            };
            HarmonizeConfig config = new HarmonizeConfig();
            config.ParentRepos.Add(parentListing);

            var childFile = new FileInfo(Path.Combine(childRepoDir.FullName, STANDARD_FILE));
            File.WriteAllText(childFile.FullName, "Child123\n");
            Commands.Stage(childRepo, childFile.FullName);
            parentListing.SetToCommit(firstParentCommit);
            File.WriteAllText(harmonizeFile.FullName, config.GetXmlStr());
            Commands.Stage(childRepo, harmonizeFile.FullName);
            childRepo.Commit(
                "A Commit",
                signature,
                signature);
            File.WriteAllText(childFile.FullName, "Child456\n");
            Commands.Stage(childRepo, childFile.FullName);
            parentListing.SetToCommit(secondCommit);
            File.WriteAllText(harmonizeFile.FullName, config.GetXmlStr());
            Commands.Stage(childRepo, harmonizeFile.FullName);
            childRepo.Commit(
                "A Commit",
                signature,
                signature);
            File.WriteAllText(childFile.FullName, "Child789\n");
            Commands.Stage(childRepo, childFile.FullName);
            childRepo.Commit(
                "A Commit",
                signature,
                signature);
            File.WriteAllText(childFile.FullName, "Child101112\n");
            Commands.Stage(childRepo, childFile.FullName);
            parentListing.SetToCommit(thirdCommit);
            File.WriteAllText(harmonizeFile.FullName, config.GetXmlStr());
            Commands.Stage(childRepo, harmonizeFile.FullName);
            childRepo.Commit(
                "A Commit",
                signature,
                signature);

            return new ConfigCheckout(parentRepo, childRepo);
        }
    }
}
