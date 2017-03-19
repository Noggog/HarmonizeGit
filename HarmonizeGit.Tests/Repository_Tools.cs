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

    class Repository_Tools
    {
        public static DirectoryInfo GetTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return new DirectoryInfo(tempDirectory);
        }

        public const string STANDARD_MERGE_BRANCH = "Merge";
        public const string STANDARD_DETATCHED_BRANCH = "Detached";
        public const string STANDARD_FILE = "Test.txt";

        public static RepoCheckout GetStandardRepo()
        {
            var date = new DateTime(2016, 03, 10);
            var signature = new Signature(
                "Justin Swanson",
                "justin.c.swanson@gmail.com",
                date);
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

    }
}
